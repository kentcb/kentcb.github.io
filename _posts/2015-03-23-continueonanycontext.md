---
title: ContinueOnAnyContext
assets: /assets/2015-03-23-continueonanycontext/
tags: [ "C#", ".NET", "TPL" ]
---
Surely one of the ugliest APIs in the Task Parallel Library is [`ConfigureAwait`](https://msdn.microsoft.com/en-us/library/system.threading.tasks.task.configureawait%28v=vs.110%29.aspx):

```csharp
public ConfiguredTaskAwaitable ConfigureAwait(bool continueOnCapturedContext)
```

Let's count the ways this is wrong:

* the name is suggestive of being widely applicable to `await`, but it is actually narrowly applicable (can only change one thing in a very specific way)
* the `continueOnCapturedContext` parameter is a `bool`, which makes invocations of `ConfigureAwait` pretty opaque unless they explicitly specify a parameter name
* .NET's default behavior is to continue on the captured context, so calling `ConfigureAwait(true)` would never be necessary. This means that whenever you see a call to `ConfigureAwait` it will *always* look like this (assuming you explicitly specify the parameter name per above): `ConfigureAwait(continueOnAnyContext: false)`

That's pretty awful. It's almost like the BCL team thought they might need to add stuff to this API later, and they didn't want to commit to an API that might therefore need to change. Even so, I feel like a flags enumeration would have been a better choice and would have been more consistent with other TPL APIs.

Anyway, I got really sick of writing (and reading) `ConfigureAwait(continueOnAnyContext: false)` so I wrote a couple of extension methods to make this nastiness go away:

```csharp
public static class TaskExtensions
{
    public static ConfiguredTaskAwaitable ContinueOnAnyContext(this Task @this)
    {
        @this.AssertNotNull(nameof(@this));
        return @this.ConfigureAwait(continueOnCapturedContext: false);
    }

    public static ConfiguredTaskAwaitable<T> ContinueOnAnyContext<T>(this Task<T> @this)
    {
        @this.AssertNotNull(nameof(@this));
        return @this.ConfigureAwait(continueOnCapturedContext: false);
    }
}
```

Now I can write this code instead:

```csharp
await SomethingAsync().ContinueOnAnyContext();
```

This makes the intent of the code far clearer, as well as making it easier to write.