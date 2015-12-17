---
title: Using the Visual Studio Test Runner for Mobile Development
assets: /assets/2015-12-17-using-the-visual-studio-test-runner-for-mobile-development/
tags: [ "Xamarin", "mobile", "testing" ]
---
One of the big challenges I've found with mobile development is the onerous code/debug cycle. Unlike regular .NET development, I can't hit **F5**, take a sip of water, and be ready for debugging. Instead, I must wait . . . and wait.

And because I insist (for good reasons) on running my unit tests on the target platforms, my [red, green, refactor](http://www.santeon.com/insight-blog/video-and-article/33-insight-blog/video-and-article/229-test-driven-development-red-green-refactor) cycle is burdened with this cost. Combined with tooling problems (debugger not working, builds failing randomly *etcetera*), it's almost untenable.

Xamarin and the community are attempting to tackle this issue. For example, Xamarin's new [Inspector](https://developer.xamarin.com/guides/cross-platform/inspector/) and Frank Krueger's [Continuous](https://github.com/praeclarum/Continuous). These tools are attempting to minimise the number of times you need to wait for the build by allowing live changes to be made to the running application.

One day I hope to build and deploy my unit test harness once, then tweak my code all day long without any fuss. But alas, Continuous does not yet support Visual Studio. And whilst Inspector purports to, I'm yet to successfully run it from VS. Until these tools mature (along with the Xamarin tool chain itself) I need a stopgap.

Hence, I decided to try a different tact altogether.

At this point you might be wondering: *why not just install xUnit's Visual Studio runner and run tests in VS? Then, once satisfied, double-check by running them on each platform.*

It's a good question, and will actually work for some scenarios. However, as soon as you take a dependency on a [bait-and-switch](http://log.paulbetts.org/the-bait-and-switch-pcl-trick/) library things fall apart. Many libraries use this trick to provide platform-specific functionality to a platform-independent consumer (a PCL). If your application is non-trivial, there's a good chance you're using a bait-and-switch library. I know I certainly do (ReactiveUI, Splat, Akavache, SQLitePCL.raw etc).

Since my tests are in a PCL, they are referencing the portable implementation of the library. The bait. And since the VS test runner simply executes those tests without any further ado, the switch never gets made. Thus, any tests that touch the bait will likely blow up with an error to the effect of "expected switch, but found bait".

There are some inelegant workarounds to this, like sharing all test code between multiple projects, each with a given target platform. But I find this approach messy and confusing. Instead what I would like is a single project for my tests and a means of executing them inside VS. Not only will this solve the problem of speed, but it will also get around Xamarin tooling problems. I can use the Visual Studio debugger to diagnose issues, and even the performance analyzer<sup>1</sup>.

Of course, I will always want to double-check things on the target platforms once I feel I'm done. But that should just be more-or-less a one-off thing at the end of a block of work.

## The Solution

To solve this problem, we need to instigate the switch ourselves and swap in the binaries for .NET proper. The easiest way I could think of achieving this is by providing a configuration file for my test library and using assembly bindings:

```XML
<?xml version="1.0" encoding="utf-8"?>

<configuration>
    <runtime>
        <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
            <dependentAssembly>
                <assemblyIdentity name="ReactiveUI"/>
                <codeBase href="net45/ReactiveUI.dll"/>
            </dependentAssembly>
        </assemblyBinding>
    </runtime>
</configuration>
```

Here I'm switching in the .NET 4.5 implementation of ReactiveUI. But notice I'm specifying a relative location. Unfortunately, the location must be within your unit test's output directory. Referencing an assembly outside the output folder requires them to be strongly-named, and the .NET community harbours a rather strong sentiment against strong-naming assemblies<sup>2</sup>.

So to ensure the assembly resides under your output directory, simply add a link to it and set the build action to **Copy if newer**:

![File Properties]({{ page.assets }}file_properties.png "File Properties")

Now I can run tests for my mobile applications using the VS test runner. There are limitations of course. Most notably, if I ever need any platform-specific tests they will reside in separate assemblies and will only be runnable on the target platform. However, my experience as these are few and far between.

Also, this is only for tests. I happen to spend most of my time in tests. But when I do get to the UI layer, this approach obviously won't help. Again, once the tool chain matures enough, I hope to ditch this approach completely and do everything with an edit and continue approach. But until then, this seems like it's going to save me a lot of time.

<sup>1</sup> Yes, I realise that performance work is very much platform dependent. However, rectifying memory leaks and algorithmic issues on one platform will certainly benefit others.
<sup>2</sup> I'm still waiting for someone of that sentiment to do a comprehensive write-up on exactly why this is.