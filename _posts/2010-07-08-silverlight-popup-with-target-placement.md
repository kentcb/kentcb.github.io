---
title: Silverlight Popup with Target Placement
assets: /assets/2010-07-08-silverlight-popup-with-target-placement/
tags: [ ".NET", "Silverlight", "XAML" ]
---
Silverlight, eh? It takes everything you knew and turns it into a confusing mish-mash of falsities, half-truths, and – just occasionally - truths.

One such falsity would be in the placement of Popup controls relative to another control. You can just set a `PlacementTarget` like in WPF, right? Nope. All you've got to work with are the `HorizontalOffset` and `VerticalOffset` properties, which specify an offset relative to the `Popup`'s parent. Not much help on their own, really.

To imbue Silverlight with the WPF goodness that is relative `Popup` placement, I've written an attached behavior that positions a `Popup` according to:

* a placement target
* any number of preferred orientations, which state how you’d like the Popup to be positioned with respect to the placement target

Usage looks like this:

{% highlight XML %}
<Popup b:PopupPlacement.PlacementTarget="{Binding ElementName=someElement}">
    <b:Popup.PreferredOrientations>
        <b:PopupOrientationCollection>
            <b:PopupOrientation Placement="Top" HorizontalAlignment="Center"/>
            <b:PopupOrientation Placement="Bottom" HorizontalAlignment="Center"/>
            <b:PopupOrientation Placement="Right" VerticalAlignment="Center"/>
            <b:PopupOrientation Placement="Right" VerticalAlignment="TopCenter"/>
        </b:PopupOrientationCollection>
    </b:Popup.PreferredOrientations>
 
    <TextBlock>My popup's contents</TextBlock>
</Popup>
{% endhighlight %}

The above will attempt to position the `Popup` centred above `someElement`. Failing that, it will attempt to place it centered below `someElement`. Failing that, it will attempt to place it with its top centred to the right of `someElement`. Failing that, it will cycle through all other possible permutations of orientations and choose the first one that fits. Failing that, it will resort to just displaying the `Popup` with your most desired orientation, even though the `Popup` doesn’t fit.

Notice how the horizontal alignment is only relevant if the `Popup` is positioned above or below the placement target, and the vertical alignment is only relevant if the `Popup` is positioned to the left or right of the placement target. Nothing stops you from specifying an irrelevant alignment, but it will be ignored.

The actual orientation chosen is exposed via the `ActualOrientation` property. You might use this (as I have in my own project – though not in the attached demo) to alter the appearance of the `Popup`’s contents.

![Sample Screenshot]({{ page.assets }}sample_screenshot.png "Sample Screenshot")

The attached download includes all the code (it's only 300 lines or so) and a demo (see above screenshot). The demo specifies only one preferred orientation, but it puts you in control of what the values are for the orientation. You can resize the space available to the `Popup` by using the grid splitters. The actual orientation chosen for is shown at the bottom. Thus, you can play with different orientations and see how the behavior differs when it has insufficient space in which to fit the `Popup`.

[Download an example project]({{ page.assets }}PopupTest.zip).

Enjoy!