---
title: Popup.StaysOpen in Silverlight
assets: /assets/2010-07-14-popupstaysopen-in-silverlight/
tags: [ ".NET", "Silverlight", "XAML" ]
---
Continuing on with the theme of restoring some WPF goodness to Silverlight, here is an attached behavior that gives Silverlight’s `Popup` control a handy `StaysOpen` property. It builds on top of the placement behavior I presented in [my last post]({% post_url 2010-07-08-silverlight-popup-with-target-placement %}), so I’ve just supplemented the demo I did for that post. Call me lazy.

Usage looks like this:

{% highlight XML %}
<Popup b:Popup.StaysOpen="False">
    <TextBlock>Popup contents</TextBlock>
</Popup>
{% endhighlight %}

The only limitations I know of are:

1. The `Popup` in question must be closed when you set the `StaysOpen` property. If, for example, your `Popup` is already open when your application first starts, events won’t hook up correctly.
2. The `Popup`’s parent is used to determine whether the user has clicked outside the bounds of the `Popup`, and therefore whether the `Popup` should be closed. So you should host the `Popup` as far up in your visual tree as appropriate, or you could add another dependency property with which you can explicitly specify the parent. I haven’t needed the latter idea yet, so haven’t bothered to add it – maybe I’ll need it later.

[Download an example project]({{ page.assets }}PopupTest.zip).

Enjoy!