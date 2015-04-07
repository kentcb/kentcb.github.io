---
title: AddTo Extension Method
assets: /assets/2015-04-07-addto-extension-method/
tags: [ "C#", ".NET", "Rx" ]
---
Continuing with the theme of extension methods that make life easier, here's one I tend to have defined in any Rx project I'm working on:

{% highlight C# %}
namespace System.Reactive.Disposables
{
    using System;
    using Kent.Boogaart.HelperTrinity.Extensions;

    public static class CompositeDisposableExtensions
    {
        public static T AddTo<T>(this T @this, CompositeDisposable compositeDisposable)
            where T : IDisposable
        {
            @this.AssertGenericArgumentNotNull(nameof(@this));
            compositeDisposable.AssertNotNull(nameof(compositeDisposable));

            compositeDisposable.Add(@this);
            return @this;
        }
    }
}
{% endhighlight %}

It's used along these lines:

{% highlight C# %}
someObservable
    .Subscribe(...)
    .AddTo(disposables);

someObservable
    .ToProperty(...)
    .AddTo(disposables);
{% endhighlight %}

Basically it makes it simple and clean to add any `IDisposable` to an existing `CompositeDisposable` as part of a reactive pipeline. The alternative is uglier:

{% highlight C# %}
disposables.Add(
    someObservable
        .Subscribe(...))

disposables.Add(
	someObservable
	    .ToProperty(...));
{% endhighlight %}

And yes, I do define it in the `System.Reactive.Disposables` namespace because that means that whenever I've imported `CompositeDisposable` I automatically get the extension method too. One less `using` statement and whole lot less annoyance.