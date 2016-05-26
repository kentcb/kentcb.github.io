---
title: Jason Smith&#39;s Xamarin Forms Performance Tips
assets: /assets/2016-05-07-jason-smith's-xamarin-forms-performance-tips/
tags: [ "Xaramin", "Xamarin.Forms", "performance" ]
---
Jason Smith, lead engineer of Xamarin Forms, gave a [great talk](https://www.youtube.com/watch?v=RZvdql3Ev0E) at this year's Evolve. He went through a long list of performance tips, as well as a Q&A session. I have not been able to find these tips in text format (slides, transcript, or blog post). In the interests of making this information more readily accessible to both me and others, I decided to summarize it here.

I've taken the liberty to rearrange and group related tips. But I want to be clear that this is entirely Jason's content, not my own.


## General

**DO** enable XAML compilation if you're using XAML:

```csharp
[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
```

**DO NOT** bind when static assignment suffices.

**DO NOT** assign default values. Doing so has a cost even though you're not modifying the value.

**AVOID** transparency. If you can achieve the same (or close enough) effect with full opacity, do so.

**DO** use `async`/`await` to avoid blocking the main thread.

**CONSIDER** inflating views off the main thread, but be sure to add it to the visual tree on the main thread. Failure to do so will not immediately crash your application, but will instead corrupt its state. Be particularly careful if you're using `MessagingCenter` in your view's constructor because it will not marshal the event to the correct thread for you.


## Layout

**DO NOT** use a `ContentView` with `Padding` just to apply a margin to the child. Instead, use the `Margin` property on the child (as of Xamarin.Forms 2.2).

**DO NOT** use a `StackLayout` to host a single child.

**DO NOT** use a `Grid` when a `StackLayout` suffices.

**DO NOT** use multiple `StackLayout`s when a `Grid` suffices.

**DO** be aware of the `Spacing` (`ColumnSpacing`/`RowSpacing` for `Grid`) and `Padding` properties. Instead of this:

```xml
<StackLayout>
    <ContentView Padding="10,10,10,5">
        <Label Text="1"/>
    </ContentView>
    <ContentView Padding="10,0,10,5">
        <Label Text="2"/>
    </ContentView>
    <ContentView Padding="10,0,10,0">
        <Label Text="3"/>
    </ContentView>
</StackLayout>
```

Do this:

```xml
<StackLayout Padding="10" Spacing="5">
    <Label Text="1"/>
    <Label Text="2"/>
    <Label Text="3"/>
</StackLayout>
```

**PREFER** using `LayoutOptions.Fill` (or `LayoutOptions.FillAndExpand`). These are the defaults and shouldn't be modified _most_ of the time.

**PREFER** star-sized grid columns/rows rather than auto-sized.

**DO NOT** use multiple `StackLayout`s to simulate a `Grid`.

**DO NOT** use `RelativeLayout` if at all possible.

**DO** pack children into the view before parents.

Instead of this:

```csharp
var page = new ContentPage();
var stackLayout = new StackLayout();
var image = new Image { Source = "person.png" };
var label = new Label { Text = "Name" };

page.Content = stackLayout;

navigationPage.Push(page);
stackLayout.Children.Add(image);
stackLayout.Children.Add(label);
```

Do this:

```csharp
var page = new ContentPage();
var stackLayout = new StackLayout();
var image = new Image { Source = "person.png" };
var label = new Label { Text = "Name" };

stackLayout.Children.Add(image);
stackLayout.Children.Add(label);
page.Content = stackLayout;

navigationPage.Push(page);
```

Note that XAML automatically packs in the correct order.

**DO** pack views in your constructor rather than `OnAppearing`.

**PREFER** animating views with the `TranslationX` and `TranslationY` properties as this avoids the need for layout.

**DO NOT** call `Layout()` (and especially `ForceLayout()`).

**CONSIDER** creating a custom layout. This is likely the best choice if your layout is simple to describe in English but difficult to implement with stock layouts, if `AbsoluteLayout` _almost_ does what you want, or if you just need raw speed.

**DO NOT** use expression-based constraints in `RelativeLayout` (and per above, try not to use `RelativeLayout` at all).

**DO NOT** use a `StackLayout` inside a `ScrollView` to simulate a `ListView`.

**DO** use a `Grid` to achieve layering.


## Labels

**DO NOT** use multiple `Label`s when one will do (using spans with `FormattedText` if necessary).

**DO** disable `Label` wrapping if possible (`LineBreakMode="NoWrap"`).

**DO NOT** change the default `VerticalTextAlignment` for `Label`s unless necessary. The default removes an entire measure cycle.

**PREFER** the `VerticalTextAlignment` and `HorizontalTextAlignment` properties of `Label` over `VerticalOptions` and `HorizontalOptions`.

**AVOID** sporadic updates to `Label`s. If the updates are to multiple `Label`s, update as a batch if possible.


## ListViews

**DO NOT** put a `ListView` inside a `ScrollView`. Use the `ListView`s `Header` and `Footer` properties instead.

**DO NOT** use `TableView` where you can use a `ListView`. Today, the only real reason to use a `TableView` is for settings-style UI where pretty much every cell has unique content.

**DO** use `ListViewCachingStrategy.RecycleElement` when you can. If it's not working, figure out why because it's worth it. This is _not_ the default.

**DO** use data template selectors to facilitate heterogeneous views within a single `ListView`. Don't override `OnBindingContextChanged` to update achieve the same effect.

**AVOID** passing `IEnumerable<T>` as a data source to `ListView`s. Instead, try to use `IList<T>`.

**DO NOT** nest `ListView`s. Instead, use groups within a single `ListView`. Nesting is explicitly unsupported and will break your application.

**DO** use `HasUnevenRows` where your `ListView` has rows of differing sizes. If the content of the cell is modified dynamically (perhaps after loading it from the database), be sure to call `ForceUpdateSize()` on the cell.


## Navigation

**DO** await the `PushAsync` and `PopAsync` methods. Failure to do so is detrimental to both performance and correctness.

**AVOID** hiding/showing the navigation bar.

**DO** use the [AppCompat backend](https://gist.github.com/jassmith/a3b2a543f99126782936) for Android. This will improve both performance and the look of the application.


## Images

**BE AWARE** that images on Android do not down-sample.

**DO** set `Image.IsOpaque` to `true` if possible.

**DO** load images from _Content_ instead of _Resources_.


## BindableProperty

**DO NOT** use the generic version of `BindableProperty.Create` (use the `string`-based version instead with C# 6's `nameof` operator).


## CarouselPage

**DO NOT** use `CarouselPage`.

**DO** use a `CarouselView` within a `ContentPage`.


## MessagingCenter

**PREFER** to use something else (e.g. Prism).

**DO** pass `MessagingCenter` either a static or instance method, not a closure/lambda expression.