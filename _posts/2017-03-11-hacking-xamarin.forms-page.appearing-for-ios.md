---
title: Hacking Xamarin.Forms' Page.Appearing for iOS
assets: /assets/2017-03-11-hacking-xamarin.forms-page.appearing-for-ios/
tags: [ "Xamarin", "Xamarin.Forms", "iOS", "ReactiveUI" ]
---
> **Note**  At the time of writing, the latest version of Xamarin.Forms is `2.3.4-pre4`. I will endeavour to update this post when the lifecycle issues are resolved.

It's no secret that Xamarin.Forms' lifecycle is terribly broken. See [here](https://forums.xamarin.com/discussion/84510/proposal-improved-life-cycle-support) and [here](https://bugzilla.xamarin.com/show_bug.cgi?id=52318) for details.

The biggest pain point I have day-to-day is that `Page.Appearing` fires too late on iOS. Instead of being triggered by `ViewWillAppear` as you'd expect, it is triggered by `ViewDidAppear`. This is a problem if you do any kind of view set-up in response to `Appearing` because your page will already be visible to the user. ReactiveUI users are particularly prone to this problem, since its activation-for-view-fetcher has no option _other_ than to use `Appearing`.

As an interim solution until this problem is fixed in Xamarin.Forms, I thought I'd try hacking around it. After a couple of false starts, I decided the only viable option - other than having your own custom build of XF, which leads to more painful problems - is to use reflection and knowledge of the default `PageRenderer` implementation. Such an approach allows us to trigger `Appearing` in response to `ViewWillAppear` and also prevent the default `PageRenderer` logic from triggering it _again_ in response to `ViewDidAppear`.

The code is as follows:

```csharp
[assembly: Xamarin.Forms.ExportRenderer(typeof(Xamarin.Forms.Page), typeof(UI.iOS.Renderers.HackedPageRenderer))]

namespace UI.iOS.Renderers
{
    using System.Reflection;
    using Xamarin.Forms;
    using Xamarin.Forms.Platform.iOS;

    // a hacky PageRenderer subclass that uses the correct hook (ViewWillAppear rather than ViewDidAppear) for the Page.Appearing event on iOS
    // TODO: remove this once XF life cycle is fixed (see https://forums.xamarin.com/discussion/84510/proposal-improved-life-cycle-support)
    public sealed class HackedPageRenderer : PageRenderer
    {
        private static readonly FieldInfo appearedField = typeof(PageRenderer).GetField("_appeared", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo disposedField = typeof(PageRenderer).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);

        private IPageController PageController => this.Element as IPageController;

        private bool Appeared
        {
            get { return (bool)appearedField.GetValue(this); }
            set { appearedField.SetValue(this, value); }
        }

        private bool Disposed
        {
            get { return (bool)disposedField.GetValue(this); }
            set { disposedField.SetValue(this, value); }
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            if (this.Appeared || this.Disposed)
            {
                return;
            }

            // by setting this to true, we also ensure that PageRenderer does not invoke SendAppearing a second time when ViewDidAppear fires
            this.Appeared = true;
            PageController.SendAppearing();
        }
    }
}
```
By including this in the iOS platform project for your Xamarin.Forms solution, your `Page.Appearing` events will now trigger from `ViewWillAppear`. Like I mentioned, this is particularly useful for ReactiveUI consumers, but frankly will help pretty much anyone doing XF+iOS.

Other renderers (e.g. `NavigationRenderer` and `TabbedRenderer`) suffer from a similar issue, and can be hacked in much the same fashion on an as-need basis.