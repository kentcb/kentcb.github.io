---
title: Custom Routing in ReactiveUI
assets: /assets/2017-04-26-custom-routing-in-reactiveui/
tags: [ "ReactiveUI", "Xamarin.Forms", "C#", "routing" ]
---
Out of the box, ReactiveUI provides some support for view model-first routing. A view model that implements `IScreen` manages a routing stack, where each view model within the stack implements `IRoutableViewModel`. In the view layer, a `RoutedViewHost` manifests a given routing stack using the idioms of the platform in question.

All this is well and good, but it does come with some limitations. Foremost, there is no support for anything specific to the platform. For example, if you want to route to a particular view on iOS _without_ animation, you can't do that because it's platform-specific. Also, if you want to show a view model in a modal context, that is not supported.

These limitations are acknowledged by the ReactiveUI team and there are plans to completely revamp the routing infrastructure at a later date (probably as part of version 9). However, I needed a richer and more reliable routing system in the here-and-now, so have implemented my own. I am often queried regarding the nature of my solution, so this post is an elaboration thereof.

Note that I work mostly with Xamarin.Forms, so my solution hinges upon that fact. However, I believe the abstractions are largely platform-agnostic, so could very well be applied in other contexts (such as Xamarin.iOS). Note also that I have catered for myself and only myself here. The point of this post is not to provide something that works perfectly well for all scenarios. It is more to demonstrate the relative ease with which a bespoke routing solution can be constructed.

## The Solution

The first moving parts in my solution are the interfaces that view models implement to participate in routing:

```csharp
public interface IModalViewModel
{
    string Id
    {
        get;
    }
}

public interface IPageViewModel
{
    string Id
    {
        get;
    }
}
```

View models can be modal or non-modal and they choose an interface accordingly. Since the interface is identical, I could collapse these two interfaces into one, but in practice I find the distinction somewhat useful to code readability. In either case, the view model simply provides a string identifier. We'll see how this is used shortly.

The next piece of the puzzle is the service itself:

```csharp
public interface IViewStackService
{
    IView View
    {
        get;
    }

    IObservable<IImmutableList<IPageViewModel>> PageStack
    {
        get;
    }

    IObservable<IImmutableList<IModalViewModel>> ModalStack
    {
        get;
    }

    IObservable<Unit> PushPage(
        IPageViewModel page,
        string contract = null,
        bool resetStack = false,
        bool animate = true);

    IObservable<Unit> PopPage(
        bool animate = true);

    IObservable<Unit> PushModal(
        IModalViewModel modal,
        string contract = null);

    IObservable<Unit> PopModal();
}

public interface IView
{
    IObservable<IPageViewModel> PagePopped
    {
        get;
    }

    IObservable<Unit> PushPage(
        IPageViewModel pageViewModel,
        string contract,
        bool resetStack,
        bool animate);

    IObservable<Unit> PopPage(
        bool animate);

    IObservable<Unit> PushModal(
        IModalViewModel modalViewModel,
        string contract);

    IObservable<Unit> PopModal();
}
```

The `IViewStackService` interface defines the API with which view models will instigate changes to the view stacks (a.k.a. the navigation stacks). Notice it provides a means of changing both the page stack, and the modal stack. Each stack is represented as an observable of a list of items.

The service itself (whose implementation we'll see shortly) is merely an admistrator of state. It is completely independent of the UI layer. It's gateway to enacting state changes in the view is via the `IView` interface, upon which its implementation depends. You'll notice that `IView` has an API similar to `IViewStackService`. The idea is that the view stack service manages the state of the stacks and dictates to the view when it should make changes to the stacks.

Here is my implementation of `IViewStackService`:

```csharp
public sealed class ViewStackService : IViewStackService
{
    private readonly ILogger logger;
    private readonly BehaviorSubject<IImmutableList<IModalViewModel>> modalStack;
    private readonly BehaviorSubject<IImmutableList<IPageViewModel>> pageStack;
    private readonly IView view;

    public ViewStackService(IView view)
    {
        Ensure.ArgumentNotNull(view, nameof(view));

        this.logger = LoggerService.GetLogger(this.GetType());

        this.modalStack = new BehaviorSubject<IImmutableList<IModalViewModel>>(ImmutableList<IModalViewModel>.Empty);
        this.pageStack = new BehaviorSubject<IImmutableList<IPageViewModel>>(ImmutableList<IPageViewModel>.Empty);
        this.view = view;

        this
            .view
            .PagePopped
            .Do(
                poppedPage =>
                {
                    var currentPageStack = this.pageStack.Value;

                    if (currentPageStack.Count > 0 && poppedPage == currentPageStack[currentPageStack.Count - 1])
                    {
                        var removedPage = PopStackAndTick(this.pageStack);
                        this.logger.Debug("Removed page '{0}' from stack.", removedPage.Id);
                    }
                })
            .SubscribeSafe();
    }

    public IView View => this.view;

    public IObservable<IImmutableList<IModalViewModel>> ModalStack => this.modalStack;

    public IObservable<IImmutableList<IPageViewModel>> PageStack => this.pageStack;

    public IObservable<Unit> PushPage(IPageViewModel page, string contract = null, bool resetStack = false, bool animate = true)
    {
        Ensure.ArgumentNotNull(page, nameof(page));

        return this
            .view
            .PushPage(page, contract, resetStack, animate)
            .Do(
                _ =>
                {
                    AddToStackAndTick(this.pageStack, page, resetStack);
                    this.logger.Debug("Added page '{0}' (contract '{1}') to stack.", page.Id, contract);
                });
    }

    public IObservable<Unit> PopPage(bool animate = true) =>
        this
            .view
            .PopPage(animate);

    public IObservable<Unit> PushModal(IModalViewModel modal, string contract = null)
    {
        Ensure.ArgumentNotNull(modal, nameof(modal));

        return this
            .view
            .PushModal(modal, contract)
            .Do(
                _ =>
                {
                    AddToStackAndTick(this.modalStack, modal, false);
                    this.logger.Debug("Added modal '{0}' (contract '{1}') to stack.", modal.Id, contract);
                });
    }

    public IObservable<Unit> PopModal() =>
        this
            .view
            .PopModal()
            .Do(
                _ =>
                {
                    var removedModal = PopStackAndTick(this.modalStack);
                    this.logger.Debug("Removed modal '{0}' from stack.", removedModal.Id);
                });

    private static void AddToStackAndTick<T>(BehaviorSubject<IImmutableList<T>> stackSubject, T item, bool reset)
    {
        var stack = stackSubject.Value;

        if (reset)
        {
            stack = new[] { item }.ToImmutableList();
        }
        else
        {
            stack = stack.Add(item);
        }

        stackSubject.OnNext(stack);
    }

    private static T PopStackAndTick<T>(BehaviorSubject<IImmutableList<T>> stackSubject)
    {
        var stack = stackSubject.Value;

        if (stack.Count == 0)
        {
            throw new InvalidOperationException("Stack is empty.");
        }

        var removedItem = stack[stack.Count - 1];
        stack = stack.RemoveAt(stack.Count - 1);
        stackSubject.OnNext(stack);
        return removedItem;
    }
}
```

As you can see, there's not a lot of complexity here. The view stack service forwards changes onto the view, and manages state for those changes. Note the need for the service to hook into a `PagePopped` observable provided by the view. A page _might_ be popped in response to a direct call against `ViewStackService.PopPage`, but it might also be popped in response to the user tapping a system-provided back button. Regardless of how the pop is instigated, the view stack service needs to be aware of it so that it can keep the page stack data in sync.

Finally, here is the view implementation. This is the part that is most specific to my scenario, because it is a Xamarin.Forms implementation:

```csharp
public sealed class MainView : NavigationPage, IView
{
    private readonly IScheduler backgroundScheduler;
    private readonly IScheduler mainScheduler;
    private readonly IViewLocator viewLocator;
    private readonly IObservable<IPageViewModel> pagePopped;

    public MainView(
        IScheduler backgroundScheduler,
        IScheduler mainScheduler,
        IViewLocator viewLocator)
    {
        Ensure.ArgumentNotNull(backgroundScheduler, nameof(backgroundScheduler));
        Ensure.ArgumentNotNull(mainScheduler, nameof(mainScheduler));
        Ensure.ArgumentNotNull(viewLocator, nameof(viewLocator));

        this.backgroundScheduler = backgroundScheduler;
        this.mainScheduler = mainScheduler;
        this.viewLocator = viewLocator;

        this.pagePopped = Observable
            .FromEventPattern<NavigationEventArgs>(x => this.Popped += x, x => this.Popped -= x)
            .Select(ep => ep.EventArgs.Page.BindingContext as IPageViewModel)
            .WhereNotNull();
    }

    public IObservable<IPageViewModel> PagePopped => this.pagePopped;

    public IObservable<Unit> PushModal(IModalViewModel modalViewModel, string contract)
    {
        Ensure.ArgumentNotNull(modalViewModel, nameof(modalViewModel));

        return Observable
            .Start(
                () =>
                {
                    var page = this.LocatePageFor(modalViewModel, contract);
                    this.SetPageTitle(page, modalViewModel.Id);
                    return page;
                },
                this.backgroundScheduler)
            .ObserveOn(this.mainScheduler)
            .SelectMany(
                page =>
                    this
                        .Navigation
                        .PushModalAsync(page)
                        .ToObservable());
    }

    public IObservable<Unit> PopModal() =>
        this
            .Navigation
            .PopModalAsync()
            .ToObservable()
            .ToSignal()
            // XF completes the pop operation on a background thread :/
            .ObserveOn(this.mainScheduler);

    public IObservable<Unit> PushPage(IPageViewModel pageViewModel, string contract, bool resetStack, bool animate)
    {
        Ensure.ArgumentNotNull(pageViewModel, nameof(pageViewModel));

        // If we don't have a root page yet, be sure we create one and assign one immediately because otherwise we'll get an exception.
        // Otherwise, create it off the main thread to improve responsiveness and perceived performance.
        var hasRoot = this.Navigation.NavigationStack.Count > 0;
        var mainScheduler = hasRoot ? this.mainScheduler : CurrentThreadScheduler.Instance;
        var backgroundScheduler = hasRoot ? this.backgroundScheduler : CurrentThreadScheduler.Instance;

        return Observable
            .Start(
                () =>
                {
                    var page = this.LocatePageFor(pageViewModel, contract);
                    this.SetPageTitle(page, pageViewModel.Id);
                    return page;
                },
                backgroundScheduler)
            .ObserveOn(mainScheduler)
            .SelectMany(
                page =>
                {
                    if (resetStack)
                    {
                        if (this.Navigation.NavigationStack.Count == 0)
                        {
                            return this
                                .Navigation
                                .PushAsync(page, animated: false)
                                .ToObservable();
                        }
                        else
                        {
                            // XF does not allow us to pop to a new root page. Instead, we need to inject the new root page and then pop to it.
                            this
                                .Navigation
                                .InsertPageBefore(page, this.Navigation.NavigationStack[0]);

                            return this
                                .Navigation
                                .PopToRootAsync(animated: false)
                                .ToObservable();
                        }
                    }
                    else
                    {
                        return this
                            .Navigation
                            .PushAsync(page, animate)
                            .ToObservable();
                    }
                });
    }

    public IObservable<Unit> PopPage(bool animate) =>
        this
            .Navigation
            .PopAsync(animate)
            .ToObservable()
            .ToSignal()
            // XF completes the pop operation on a background thread :/
            .ObserveOn(this.mainScheduler);

    private Page LocatePageFor(object viewModel, string contract)
    {
        Ensure.ArgumentNotNull(viewModel, nameof(viewModel));

        var view = viewLocator.ResolveView(viewModel, contract);
        var viewFor = view as IViewFor;
        var page = view as Page;

        if (view == null)
        {
            throw new InvalidOperationException($"No view could be located for type '{viewModel.GetType().FullName}', contract '{contract}'. Be sure Splat has an appropriate registration.");
        }

        if (viewFor == null)
        {
            throw new InvalidOperationException($"Resolved view '{view.GetType().FullName}' for type '{viewModel.GetType().FullName}', contract '{contract}' does not implement IViewFor.");
        }

        if (page == null)
        {
            throw new InvalidOperationException($"Resolved view '{view.GetType().FullName}' for type '{viewModel.GetType().FullName}', contract '{contract}' is not a Page.");
        }

        viewFor.ViewModel = viewModel;

        return page;
    }

    private void SetPageTitle(Page page, string resourceKey)
    {
        var title = Localize.GetString(resourceKey);
        page.Title = title;
    }
}
```

Crucially, our view extends `NavigationPage`, since that is the page type that gives us navigation capabilities in Xamarin.Forms. From there, we must also implement `IView`.

Perhaps the trickiest aspect of this code is in dealing with Xamarin.Forms' idiosynchrasies. Specifically, a root page is required before any attempt is made to realize the view on-screen. This wouldn't be a problem except that I also want to create views off the main thread to increase perceived performance. If we create views on a background thread, that leaves the UI thread free to show a spinner or the like, thereby mitigating the risk of nasty lags as the user switches between views. However, if we create the very first page off the UI thread, we have a race condition. Xamarin.Forms expects there to be a root page when it first attempts to render the view, but our background thread may still be in the process of creating it. Therefore, all views are created on a background thread _except_ the root view.

Another annoyance is in dealing with resetting the stack. Xamarin.Forms has the ability to pop to the root view, but not to completely supplant the root view with another. To that end, we have to get a bit tricky by inserting our new root view and popping to that instead.

Other than those warts, the code is quite clean.

Notice the `SetPageTitle` method, which uses the `Id` of the page (or modal) as a key with which to look up a resource to use as the title. The `Localize` class is outside the scope of this post, but it's exactly what you'd expect. You can always replace it with something else. If your application isn't localized, you could even just use the actual titles as the IDs, though I wouldn't recommend it.