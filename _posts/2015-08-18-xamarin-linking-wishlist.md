---
title: Xamarin Linking Wishlist
assets: /assets/2015-08-18-xamarin-linking-wishlist/
tags: [ "Xamarin", "linking" ]
---
For me, one of the more painful aspects of mobile development using the Xamarin tool chain is linking.

Linking is the process of culling any unused binary code from your application. If you use a single type from a large assembly, your application will include the entire assembly unless you link. If you use small portions of many different assemblies, you can imagine how quickly the cost adds up. And bear in mind that this includes BCL assemblies such as *System.dll* (≃1.5MB) and *System.Core.dll* (≃700KB).

As an example, my most recent linking endeavor reduced my IPA from 45.5MB down to just over 15.

But, boy, was it **painful**. The process I followed was:

1. Start with a very forgiving linker configuration by individually preserving all assemblies. Ensure the application builds and runs successfully.
2. Remove the preservation configuration for one of the assemblies.
3. Build, run, and test the application.
4. If OK, go to 2.
5. If not OK, ascertain which type/member is required and update the configuration accordingly.
6. Go to 3.

As I was laboring through the linking process, I couldn't help but imagine ways in which things could be made easier with the requisite investment from Xamarin. I wanted to document them here with the hope of inspiring better tooling.

### Improve Error Messages

Step 5 in my process (*ascertain which type/member is required*) isn't always easy. Sometimes you'll get a helpful error message making it clear that a type could not be found - be grateful when this is the case. Unfortunately, the manifestation of a missing type or member is often far more insidious.

Consider, for example, what happens when I fail to preserve [Splat](https://github.com/paulcbetts/splat) from linking in my application:

```
System.TypeInitializationException: An exception was thrown by the type initializer for Splat.LogHost ---> System.ExecutionEngineException: Attempting to JIT compile method 'Splat.LogHost:.cctor ()' while running with --aot-only. See http://docs.xamarin.com/ios/about/limitations for more information.
```
Fun fact: the [URL](http://docs.xamarin.com/ios/about/limitations) in the error message doesn't appear to exist anymore. It seems it should instead be pointing you to [this page](http://developer.xamarin.com/guides/ios/advanced_topics/limitations/).

Anyway, it looks like something in the `LogHost` static constructor is instigating JIT compilation, which shouldn't be happening when building with the `--aot-only` switch. And said switch is necessary on iOS because JIT'ing is simply not allowed by Apple, bless them.

The error is suggestive of there being an AOT limitation thwarting our application from running, when in fact it's overly-aggressive linking. And to understand what needs to be preserved from linking, we need to go spelunking through the Splat code and attempt to figure out what is triggering the JIT request. And if we ever figure *that* out, things could change when we later decide to switch to a different version of Splat.

And don't be thinking this kind of error only occurs with Splat. I had the exact same problem with the *System.Reactive.Linq* assembly.

The point is, wouldn't it be nice if overly-aggressive linking resulted in consistent, easy to understand errors? I realize this is a hard problem because the run-time has no idea that code has been removed that could otherwise have enabled the application to run, but I'm sure a healthy set of linking scenarios could be pooled and examined for ways in which errors can be improved.

### Better Linker Documentation

Configuring the linker is currently a case of creating an XML file, googling for whatever information one can find on the format, and generally getting frustrated. Even [Xamarin's documentation](http://developer.xamarin.com/guides/cross-platform/advanced/custom_linking/) doesn't demonstrate some very common scenarios, such as:

* preserving an entire assembly
* preserving an entire type
* preserving an event 

For what it's worth, the syntax for these scenarios looks like this:

```XML
<!-- preserve entire Splat assembly -->
<assembly fullname="Splat">
    <type fullname="*"/>
</assembly>

<!-- preserve the LogHost type in the Splat assembly -->
<assembly fullname="Splat">
    <type fullname="Splat.LogHost"/>
</assembly>

<!-- preserve the UIBarButtonItem.Clicked event -->
<assembly fullname="Xamarin.iOS">
    <type fullname="UIKit.UIBarButtonItem">
        <method name="add_Clicked"/>
        <method name="remove_Clicked"/>
    </type>
</assembly>
```

Note the inconsistency between preserving an entire assembly, where you are required to specify a type wild-card, and preserving an entire type, where you are not required to specify a member wild-card.
 
Whilst this kind of thing can be inferred from the [documentation](http://linux.die.net/man/1/monolinker) and [code](https://github.com/mono/mono/tree/master/mcs/tools/linker), it would be nice if common use cases were covered in greater detail, especially those likely to trip up the Xamarin developer.

### Editor support for Linker Configuration

A well-designed, dedicated editor for the linker XML has the potential to significantly reduce the friction associated with configuring linking.

At its simplest, I could imagine a tree view with tri-state check boxes:

![Linking Editor]({{ page.assets }}linking-editor.png "Linking Editor")

*(limitations of the mock-up tool I used prevent me from accurately depicting tri-state checkboxes, among other things, so please use your imagination)*

Of course, that doesn't allow for wild-cards . A details pane on the right for any selected assembly or type could address that.

The point is, by providing an editor, I no longer have to:

* figure out exact assembly, type, and member names
* type them myself with the very real risk of confounding typos
* understand the syntax being generated for me

### Auto-generation of Linker Configuration

What would be Super Amazing would be to be able to run a non-linked instance of your application, put it through its paces, and have linker configuration generated automatically based on that. The generator should be smart enough only to preserve those types and members that would not otherwise be preserved.

In other words, a tool that collects a set *A* of types and members that are used at run-time, a set *B* of types and members that are successfully resolved through static analysis, and thus calculates a set *C* as the difference between those two. Then, using set *C*, it generates the appropriate linker configuration.

Were this tool available, one could imagine writing and maintaining a single UI test that walks through your entire application. When you wanted to update linker configuration (such as just prior to release), you would run this UI test via the linker configuration generator tool. You'd go and get yourself a cup of tea, and return to find a minimal linker configuration that is guaranteed to work for those parts of the application exercised by your test.