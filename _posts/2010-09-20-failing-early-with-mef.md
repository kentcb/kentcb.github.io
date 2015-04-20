---
title: Failing Early with MEF
assets: /assets/2010-09-20-failing-early-with-mef/
tags: [ ".NET", "MEF" ]
---
Several weeks back I had the pleasure of having [Glenn Block](http://blogs.msdn.com/b/gblock/) over for dinner and discourse. I showed him a project I've been working on, and naturally our conversation centred around my use of MEF and REST. Glenn asked that I do a couple of posts on some of the MEF stuff I had done, so this is the first one.

One of the things I frequently found happening when using MEF was:

1. Make a few simple changes
2. Run the application
3. Get a runtime exception
4. Trawl through the hierarchy of exceptions, only to find that it was a simple composition problem

More often than not, I had just forgotten to add the appropriate metadata to my types. Since this is something that can be caught early, I felt compelled to do exactly that. However, I also wanted to ensure that this verification did not result in a bloated release deployment. I just wanted something to aid the developer whilst building the application.

I came up with this:

```C#
private static void VerifyComposition()
{
#if DEBUG
    var compositionInfo = new CompositionInfo(rootCatalog, container);
     
    using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
    {
        CompositionInfoTextFormatter.Write(compositionInfo, stringWriter);
        var compositionDetail = stringWriter.ToString();
        var errorDetected = compositionDetail.Contains("[Primary Rejection]");
         
        if (errorDetected)
        {
            Debugger.Break();
            Debug.Assert(false, "Composition error detected:" + Environment.NewLine + Environment.NewLine + compositionDetail);
        }
    }
#endif
}
```

This uses the `CompositionInfo` type, which is included in the *Microsoft.ComponentModel.Composition.Diagnostics* assembly. As an aside, I had to [build my own version](http://mef.codeplex.com/Thread/View.aspx?ThreadId=217035) of this assembly for use with Silverlight since there is none provided out of the box.

The `CompositionInfo` class is used to dump detailed diagnostics of composition into a string. The string is then examined for any issues by searching for `"[Primary Rejection]"`. If any error is detected, I break into the debugger so the developer can examine the composition dump. This means the workflow above is replaced with this one (same number of steps, but far less time-consuming):

1. Make a few simple changes
2. Run the application
3. Debugger breaks letting me know there is a composition problem
4. Examine `compositionDetails` in the debugger to get the details of the composition error

As Glenn pointed out, it may be that you want to tweak the string search according to your particular project. In my case, I want any composition problem at all to be detected ASAP and to bail out.

Notice also that this whole affair is wrapped in an `#if DEBUG` block. Not only does this prevent this code itself from appearing in a release build, it also prevents the *Microsoft.ComponentModel.Composition.Diagnostics* assembly from appearing in your release output (assuming you don't use it elsewhere). This was particularly important for me because it's a Silverlight app, and I wanted the download size to be as small as possible.

I hope this is of some use.