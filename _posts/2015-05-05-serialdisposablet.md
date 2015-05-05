---
title: SerialDisposable&lt;T&gt;
assets: /assets/2015-05-05-serialdisposablet/
tags: [ ".NET", "Rx" ]
---
Reactive Extensions includes a super useful class called `SerialDisposable`. The idea is you can assign any `IDisposable` to it and it automatically disposes of any previously assigned value. Suppose, for example, you have a need to create a `UIPopoverController` whenever some user interaction occurs:

```C#
private readonly SerialDisposable popoverDisposable = new SerialDisposable();
private UIPopoverController searchPopover;

private UIPopoverController SearchPopover
{
    get { ... }
    set { ... }
}

// elsewhere in the code
someUserInteractionObservable
    .Select(_ => new UIPopoverController(...))
    .Subscribe(
        x =>
        {
            popoverDisposable.Disposable = x;
            this.SearchPopover = x;
        });
```

Here we ensure that any previously created `UIPopoverController` is disposed of by assigning the newly created one to the `Disposable` property of our `SerialDisposable`. But notice how we also assign the same value to our `SearchPopover` property. That's because `SerialDisposable.Disposable` is of type `IDisposable`. That is, by storing our `UIPopoverController` inside a `SerialDisposable`, we've lost some compile-time type information.

OK, so the example is contrived and incomplete but you get the point.

When faced with this situation of either doubling-up on backing fields versus sprinkling the code with casts, I figured I'd just create a generic `SerialDisposable<T>` class instead:

```C#
namespace System.Reactive.Disposables
{
    using System;

    // generic variant of Rx's SerialDisposable class
    public sealed class SerialDisposable<T> : ICancelable, IDisposable
        where T : IDisposable
    {
        private readonly SerialDisposable disposable;

        public SerialDisposable()
        {
            this.disposable = new SerialDisposable();
        }

        public bool IsDisposed => this.disposable.IsDisposed;

        public T Disposable
        {
            get { return (T)this.disposable.Disposable; }
            set { this.disposable.Disposable = value; }
        }

        public void Dispose()
            => this.disposable.Dispose();
    }
}
```

`SerialDisposable` is sealed, so we're forced to use composition instead of inheritance, but [we were going to do that anyway](http://en.wikipedia.org/wiki/Composition_over_inheritance), right?

The upshot is that now we can just declare a single field of type `SerialDisposable<UIPopoverController>` and utilize that wherever relevant:

```C#
private readonly SerialDisposable<UIPopoverController> searchPopover = ...;

private UIPopoverController SearchPopover
{
    get { return this.searchPopover.Disposable; }
    set
    {
        this.searchPopover.Disposable = value;
        // plus raise property changed if necessary
    }
}

// elsewhere in the code
someUserInteractionObservable
    .Select(_ => new UIPopoverController(...))
    .Subscribe(x => this.SearchPopover = x);
```

I've been pondering as well whether it would be a Good Idea™ to have `SerialDisposable<T>` also implement `IObservable<T>`, where each instance assigned to the `Disposable` property ticks the observable. As yet, the jury's out.