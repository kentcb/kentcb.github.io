---
title: Ignore Extension Method
assets: /assets/2015-04-20-ignore-extension-method/
tags: [ "C#", "TPL" ]
---

Sometimes when calling an asynchronous method, it can be useful to throw away the resultant `Task`. Generally this is a bad idea and can be resolved by a change in design, but just occasionally it's genuinely useful. However, if we do invoke an asynchronous method and discard the returned `Task`, the compiler issues a warning. This code, for example:

```C#
public SomeViewModel()
{
	this.InitializeAsync();
}
```

Results in this warning:

```
Warning	CS4014
Because this call is not awaited, execution of the current method continues before the call is completed.
Consider applying the 'await' operator to the result of the call.
```

In this case we're in a constructor, so we cannot apply the `await` operator as suggested by the compiler. So what are we to do? We can just leave the warning message there as a reminder to clean up later, but that's a dangerous practice in my experience because once warning messages become an expected output of the build, they are no longer heeded in general.

We could also wrap the invocation in a `#pragma`:

```C#
#pragma warning disable 4014

    this.InitializeAsync();

#pragma warning restore 4014
```

But this is both ugly and it lacks clarity. One needs to know what warning 4014 is before the code can be understood.

Instead of these options, I prefer to define my own `Ignore` extension method:

```C#
public static class TaskExtensions
{
    public static void Ignore(this Task @this)
    {
        @this.AssertNotNull(nameof(@this));
    }

    public static void Ignore<T>(this Task<T> @this)
    {
        @this.AssertNotNull(nameof(@this));
    }
}
```

I can then use it as follows:

```C#
this
    .InitializeAsync()
    .Ignore();
```

This code avoids the compiler warning and is more self-descriptive to boot.

Again, I want to stress that we can generally solve these kinds of issues by improving our design. In this case, we could require clients invoke (and await) our `InitializeAsync` method themselves. However, in those situations where an alternative design is not readily available, the `Ignore` extension method allows me to proceed and gives me a single place to look when I later return to improve the design (I can simply find all references to the `Ignore` method). So it's definitely a case of "use with caution and understanding".