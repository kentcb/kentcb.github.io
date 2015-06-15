---
title: The Peril of VirtualTimeScheduler.Start
assets: /assets/2015-06-14-the-peril-of-virtualtimescheduler.start/
tags: [ ".NET", "Rx", "reactive-extensions" ]
---
Pop quiz, hot-shot: does the following test pass or fail?

```C#
[Fact]
public void theres_a_bomb_on_the_bus()
{
    var scheduler = new TestScheduler();
    var count = 0;
    Observable
        .Timer(
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromMilliseconds(10),
            scheduler)
        .Subscribe(_ => ++count);

    scheduler.Start();

    Assert.True(count > 0);
}
```

Well? Seems pretty clear it should pass, right?

It's actually a trick question (how devious of me, I know). In actuality, this test *never* completes. And because I've wasted hours of my life tracking this issue down different times in multiple projects, I figure it's time to blog about it. Perhaps next time I won't be bitten.

The problem is fairly simple to identify once distilled to a basic form such as that shown above. But when you're unit testing real-world code that you're not even certain works yet, it can get horribly confusing to identify.

The documentation for the `Start` method on `TestScheduler` (which is actually inherited from its base class, `VirtualTimeScheduler`) helpfully states:

> Starts the virtual time scheduler.

OK, *thanks* [Ghostdoc](http://submain.com/products/ghostdoc.aspx). Obviously, it's not at all clear what that means until we look at [the code](https://github.com/Reactive-Extensions/Rx.NET/blob/a13e3ff05bdded5cef2bf40bface22f8fa4ae316/Rx.NET/Source/System.Reactive.Linq/Reactive/Concurrency/VirtualTimeScheduler.cs). Here's the essence:

```C#
do
{
    var next = GetNext();

    if (next != null)
        next.Invoke();
    else
        IsEnabled = false;
} while (IsEnabled);
```

Spot the problem now? **The `Start` method keeps on running until there are no more messages left to process.**

If any message processed by `Start` schedules another message, that second message will also be processed before `Start` completes. And if that happens recursively (message A schedules message B, which schedules message C, which schedules...) then `Start` will *never* complete.

And, of course, that's *exactly* the kind of behavior we'd expect from `Observable.Timer`. When the first message scheduled by `Timer` executes, it needs to schedule the next, which needs to schedule the next, and so on *ad nauseam*.  

Perhaps a better name for `Start` - one more evocative of its actual function - would be `AdvanceUntilEmpty`. Indeed, I am considering defining an extension method of this name which simply invokes `Start`. At least that way the code is clearer, and a bright orange warning light will flash in my mind each time I invoke it.

So how do we fix our test? In this contrived case we have direct access to the observable produced by `Timer`, so we could take one of these approaches:

* before we call `Start`, schedule our own future message to dispose of the timer subscription.
* use the like-named-but-completely-unrelated `Start` methods defined by `TestScheduler` itself. By passing an observable to one of these overloads along with timing information as to when it should create/subscribe/dispose of the subscription, we can simulate any timing we like and impose control over when the sequence is terminated. Note that we'd have to change the `Subscribe` call in our test to `Do` for this to compile.

However, in reality we're unlikely to have access to the observable. It's probably an implementation detail of an application component, such as a view model. Therefore we can't use these approaches. To the best of my knowledge, the best solution is to forgo `Start` altogether and instead use the `AdvanceBy` method:

```C#
[Fact]
public void theres_a_bomb_on_the_bus()
{
    var scheduler = new TestScheduler();
    var count = 0;
    Observable
        .Timer(
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromMilliseconds(10),
            scheduler)
        .Subscribe(_ => ++count);

    scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

    Assert.True(count > 0);
}
```

As you can see, we're now using `AdvanceBy` to ensure that we only execute one second's worth of messages. Even if the scheduler has more messages after a second has passed, it won't bother to process them.

> **Pro-tip**: define an `AdvanceBy` extension method that takes `TimeSpan` instead of `long`, and an `AdvanceTo` extension method that takes `DateTime` instead of `long`. Using ticks for timing information leads to tests that are harder to read and easier to screw up.

The moral of this blog post is to beware of the effect of a call to `VirtualTimeScheduler.Start`, especially when combined with operators such as `Observable.Timer` where each message queues a subsequent message. If your system under test includes any calls to `Observable.Timer` that are running under your test scheduler (which they should be because otherwise how would you reliably test?) then don't call `Start` in your test. 

PS. I will submit a link to this post to the Rx community to gather feedback.