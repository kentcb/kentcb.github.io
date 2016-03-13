---
title: Hosting Xamarin Forms Controls in Native Views
assets: /assets/2015-07-21-hosting-xamarin-forms-controls-in-native-views/
tags: [ "Xamarin", "Xamarin Forms", "iOS", "C#", ".NET" ]
---
**NOTE: the approach recommended in this blog post is based on Xamarin Forms 1.4.3.6376. If you're using a newer version, it may be worth investigating whether the fundamental limitation discussed in this post has been addressed.**

Recently I was working on a Xamarin Forms application and needed a control that acted as a container for other controls, and added behavior around that containment. There was nothing provided out of the box for the behavior I desired, so I resolved to writing my own.

For the purposes of this post, pretend I wanted a control that allows users to flick between any number of child controls. We might define it like this:

```csharp
[ContentProperty("Children")]
public class FlickView : View, IViewContainer<View>
{
    private readonly IList<View> children;

    public FlickView()
    {
        this.children = new List<View>();
    }

    public IList<View> Children
    {
        get { return this.children; }
    }
}
```

In the iOS renderer for our `FlickView` control, we'd like to place each child's native view inside a `UIScrollView`. We would do so using code like this:

```csharp
public class FlickViewRenderer : ViewRenderer<FlickView, UIView>
{
    private UIScrollView scrollView;
    private IList<IVisualElementRenderer> childRenderers;

    protected override void OnElementChanged(ElementChangedEventArgs<FlickView> e)
    {
        base.OnElementChanged(e);

        if (e.OldElement != null)
        {
            this.scrollView.Dispose();

            foreach (var childRenderer in this.childRenderers)
            {
                childRenderer.NativeView.RemoveFromSuperview();
                childRenderer.NativeView.Dispose();
                childRenderer.Dispose();
            }
        }

        if (e.NewElement != null)
        {
            this.scrollView = new UIScrollView
            {
                PagingEnabled = true,
                ShowsHorizontalScrollIndicator = false,
                ShowsVerticalScrollIndicator = false,
                ScrollsToTop = false
            };

            this.childRenderers = e
                .NewElement
                .Children
                .Select(RendererFactory.GetRenderer)
                .ToList();

            foreach (var childRenderer in this.childRenderers)
            {
                this.scrollView.AddSubview(childRenderer.NativeView);
            }

            this.SetNativeControl(this.scrollView);
        }
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();

        this.scrollView.Frame = this.Bounds;

        this.scrollView.ContentSize = new CGSize(
            this.scrollView.Frame.Width * this.childRenderers.Count,
            this.scrollView.Frame.Height);

        for (var i = 0; i < this.childRenderers.Count; ++i)
        {
            var childRenderer = this.childRenderers[i];
            var childFrame = this.scrollView.Bounds;
            childFrame.Offset(i * this.scrollView.Frame.Width, 0);
            childRenderer.NativeView.Frame = childFrame;

            childRenderer.Element.Layout(
                new Rectangle(
                    0,
                    0,
                    this.Bounds.Width,
                    this.Bounds.Height));
        }
    }
}
```  

Our renderer's `OnElementChanged` method creates a `UIScrollView` with the necessary properties set so that it behaves in a page-like manner. It then gets renderers for each child, which allows us to bridge the gap between Xamarin Forms' platform-agnostic control and a corresponding native iOS control. The native view exposed by each renderer is then added as a child view of the `UIScrollView`. Finally, a call to `SetNativeControl` is made against the renderer, passing in the `UIScrollView` we created.

The `LayoutSubviews` method assigns the bounds of the renderer to the `UIScrollView`, ensuring it occupies all available space. The `ContentSize` of the `UIScrollView` is set according to the number of child views, with the assumption that each child will take up the entire space occupied by the `FlickControl`. Then, the native view for each child has its `Frame` set such that each appears "one page" after the other in a horizontal strip. Finally, the `Layout` method is called against the renderer's Xamarin Forms element so that it is aware of the space it is occupying.

With our control in place, we can define a quick UI with which to test it:

```xml
<?xml version="1.0" encoding="UTF-8"?>

<ContentPage
        xmlns="http://xamarin.com/schemas/2014/forms"
        xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
        xmlns:local="clr-namespace:HostingXFControlsInNativeViews"
        x:Class="HostingXFControlsInNativeViews.MainPage">
    <local:FlickView>
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="*"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <Label Grid.Row="1" Text="Hello," HorizontalOptions="Center"/>
            <Label Grid.Row="2" Text="World!" HorizontalOptions="Center"/>
        </Grid>
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="*"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <Label Grid.Row="1" Text="Goodbye" HorizontalOptions="Center"/>
            <Label Grid.Row="2" Text="Cruel World!" HorizontalOptions="Center"/>
        </Grid>
    </local:FlickView>
</ContentPage>
```

If we start up an app using the above code, we see...precisely nothing. And if we use the excellent [Reveal](http://revealapp.com/) to take a peek at our visual tree, we can confirm why:

![Our interface in Reveal]({{ page.assets }}reveal.png "Our interface in Reveal")

As you can see, our labels *are* in the visual tree, but their frames are set such that they are effectively hidden.

At this point in the process of writing my custom control I tried a dozen different code approaches, debugged for hours, and read countless forum posts and documentation pages. All of it was to no avail - I was simply stuck, seemingly doing everything I should reasonably need to do, but unable to get the child views to appear.

Over the course of a week or two, I exchanged several emails with Xamarin support. Eventually I was able to come up with a solution based on something the support engineer mentioned. It ain't pretty, but it works.

The problem is that each Xamarin Forms element needs to know its platform. By that I mean it needs its `Platform` property set to an instance of `IPlatform`. Every `Element` has a `Platform` property for this purpose, but the problem is that it is currently `internal`. Until the fine folks at Xamarin see fit to open this API up to us plebs, this forces us to use reflection to solve our problem. In `OnElementChanged` we need to do this:

```csharp
...

// HACK: we have to explicitly pass the platform through to each child element
var platformProperty = typeof(Element)
    .GetProperty("Platform", BindingFlags.NonPublic | BindingFlags.Instance);
var platform = platformProperty
    .GetValue(this.Element);

foreach (var childRenderer in this.childRenderers)
{
    this.scrollView.AddSubview(childRenderer.NativeView);

    // HACK: we have to explicitly pass the platform through to each child element
    platformProperty.SetValue(childRenderer.Element, platform);
}

...
```

With this <s>change</s> hack in place, our child views now appear correctly:

![Flick view animation]({{ page.assets }}flick-view.gif "Flick view animation")

You can download a working sample project [here]({{ page.assets }}project.zip).