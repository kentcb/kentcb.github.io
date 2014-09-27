---
title: WPF Splash Screens
assets: /assets/2012-12-24-wpf-splash-screens/
tags: [ ".NET", "WPF" ]
---
It may seem odd to blog about something as mundane as WPF splash screens. After all, it's a solved problem, right? Wrong. I think there are various problems that come about by using standard splash screen helpers, and I want to address those here.

First though, we need to get back to basics by discussing what the purpose of a splash screen is. Contrary to many a splash you may have been subjected to, the purpose of a splash screen is not to annoy users or to wow business folks. To quote the [repository of all knowledge](http://en.wikipedia.org/wiki/Splash_screen):

> Splash screens are typically used by particularly large applications to notify the user that the program is in the process of loading. They provide feedback that a lengthy process is underway.

So a splash screen's primary goal is to reassure the user that their choice of software is, indeed, loading. That implies that the splash screen must appear quickly, otherwise the user won't feel reassured at all and may end up launching the software a second time. A splash screen might also provide some additional pieces of information about the software (such as the version number).

With those points in mind, it's time to move onto specifics.

## The SplashScreen Class

WPF provides a [`SplashScreen`](http://msdn.microsoft.com/en-us/library/system.windows.splashscreen.aspx) class. It is simple by design and addresses the main goal of splash screens: immediate feedback. By virtue of forgoing the WPF stack and instead relying on [Windows Imaging Component](http://msdn.microsoft.com/en-gb/library/windows/desktop/ee719654(v=vs.85).aspx) (WIC) to display images, it provides the quickest path to getting a splash on the screen short of writing your own native bootstrapper.

Of course, it has limitations, otherwise this blog post wouldn't have much meat on it. We'll get to those shortly...

## Disabling the Splash

Within mere minutes of adding a splash to your application, you'll be sick of seeing it, especially if your artistic skills are on par with mine. And when the debugger breaks with the splash screen superimposed over it, you'll be screaming abuse at Thaygorn (the god of splash screens, as ordained by me, just now).

So the first thing you should do is provide a command-line switch to disable the splash screen. I like `-thaybegorn`, but heathens may prefer `-nosplash`. For WPF applications, this means forgoing the generated entry point and providing your own. For any non-trivial application, you'll likely want your own entry point anyway, so that you can bootstrap logging and hook into unhandled exception events as early as possible.

In your entry code, you can have something like this:

{% highlight C# %}
internal static void Main(string[] args)
{
    var showSplash = !args.Any(x => string.Equals("-nosplash", x, StringComparison.OrdinalIgnoreCase));
     
    if (showSplashScreen)
    {
        ShowSplashScreen();
    }
}
 
private static void ShowSplashScreen()
{
    splashScreen = new SplashScreen("Splash.png");
    splashScreen.Show(true, true);
}
{% endhighlight %}

Now developers can simply configure their IDE to pass in this command line argument when starting the application. Most of the time, they can live in blissful ignorance of there even being a splash screen.

## Regaining some Dynamism

One of the biggest things you give up by using `SplashScreen` is dynamic content. No animations (not even animated GIFs), no progress information, no nothing. If you're like me, you'd at least like the version number of the product in the splash screen. But that means you have to modify your splash image every time your version changes. Rather than doing this manually (ugh!), you should look to your build process to do it for you. I assume you already have a shared assembly info file with the version of your product in it? If not, go do that right now.

Once your build has access to a well-defined version number, you can use a custom task to modify a base splash image on the fly. Here's one I wrote (MSBuild):

{% highlight XML %}
<UsingTask TaskName="AddTextToImage" TaskFactory="CodeTaskFactory" AssemblyFile="$(MSBuildToolsPath)\Microsoft.Build.Tasks.v4.0.dll">
    <ParameterGroup>
        <InputPath ParameterType="System.String" Required="true" />
        <TopRightPoint ParameterType="System.String" Required="true" />
        <OutputPath ParameterType="System.String" Required="true" />
        <IsBitmap ParameterType="System.Boolean" Required="false" />
        <Text ParameterType="System.String" Required="true" />
    </ParameterGroup>
    <Task>
        <Reference Include="WindowsBase"/>
        <Reference Include="PresentationCore"/>
        <Reference Include="PresentationFramework"/>
        <Reference Include="System.Xaml"/>
         
        <Using Namespace="System" />
        <Using Namespace="System.Globalization" />
        <Using Namespace="System.IO" />
        <Using Namespace="System.Windows" />
        <Using Namespace="System.Windows.Media" />
        <Using Namespace="System.Windows.Media.Imaging" />
         
        <Code Type="Fragment" Language="cs"><![CDATA[
            var originalImageSource = BitmapFrame.Create(new Uri(InputPath));
            var visual = new DrawingVisual();
 
            using (var drawingContext = visual.RenderOpen())
            {
                drawingContext.DrawImage(originalImageSource, new Rect(0, 0, originalImageSource.PixelWidth, originalImageSource.PixelHeight));
 
                var typeFace = new Typeface(new FontFamily("Century Gothic"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
                var formattedText = new FormattedText(Text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeFace, 8, Brushes.White);
                var topRightPoint = Point.Parse(TopRightPoint);
                var point = new Point(topRightPoint.X - formattedText.Width, topRightPoint.Y);
 
                drawingContext.DrawText(formattedText, point);
            }
 
            var renderTargetBitmap = new RenderTargetBitmap(originalImageSource.PixelWidth, originalImageSource.PixelHeight, originalImageSource.DpiX, originalImageSource.DpiY, PixelFormats.Pbgra32);
 
            renderTargetBitmap.Render(visual);
 
            var bitmapFrame = BitmapFrame.Create(renderTargetBitmap);
            BitmapEncoder encoder = null;
             
            if (IsBitmap)
            {
                encoder = new BmpBitmapEncoder();
            }
            else
            {
                encoder = new PngBitmapEncoder();
            }
             
            encoder.Frames.Add(bitmapFrame);
 
            using (var stream = File.OpenWrite(OutputPath))
            {
                encoder.Save(stream);
            }
            ]]>
        </Code>
    </Task>
</UsingTask>
{% endhighlight %}

In my case, I need to ensure the text within the splash image is right-aligned, so I have a `TopRightPoint` property. I use it like this:

{% highlight XML %}
<AddTextToImage InputPath="$(ResourcesPath)/SplashTemplate.png" OutputPath="$(ResourcesPath)/Splash.png" TopRightPoint="350,115" Text="$(Version)"/>
{% endhighlight %}

This takes the *SplashTemplate.png* image, adds the contents of the `Version` property at the location specified, then saves it to *Splash.png*. Easy. This step, of course, happens before I build the application, so that the modified *Splash.png* is included in the executable as a resource. I also check in a copy of the splash template as *Splash.png* so that developers don't run into missing file issues if they compile without the splash generation step having been executed. I then set up my source control to ignore any changes to that file.

I use the same technique to add version numbers to my setup images. You could take it a lot further (add other text, flexible fonts, superimpose other images), of course, but I haven't had any need.

## Postponing Closure

The `SplashScreen` class supports an auto-close function, which will start closing the splash screen as soon as the application is loaded (ie. the dispatcher is processing messages with a priority of `DispatcherPriority.Loaded`). It also allows you to close the splash screen yourself, via its [`Close`](http://msdn.microsoft.com/en-us/library/system.windows.splashscreen.close.aspx) method.

In my experience, a splash screen closing as soon as the application is loaded can be a jarring experience. The application being loaded does not imply that all windows have been initialized and displayed. You may have some background process initializing services or connections or whatever, with UI initializing asynchronously in respect to that.

I much prefer to have explicit control over when the splash screen closes. I like to have it wait a couple of seconds after the application has loaded before fading out the splash screen. And sometimes I need even more control than that, such as waiting for some specific UI initialization event.

Simply starting a timer in our entry code does not suffice, because it will start before the application has loaded. Indeed, if the application takes more than a couple of seconds to load, the splash may close before the UI even appears! And since the entry code is the main UI thread, we can't just block until the application initializes, because then it never will!

One approach might be to spin off another thread or task to periodically check whether the application is loaded. However, there's a cleaner way via WPF's [`DispatcherFrame`](http://kentb.blogspot.co.uk/2008/04/dispatcher-frames.html) mechanism. What we can do is pump the dispatcher until the application is loaded:

{% highlight C# %}
private static void PumpDispatcherUntilPriority(DispatcherPriority dispatcherPriority)
{
    var dispatcherFrame = new DispatcherFrame();
    Dispatcher.CurrentDispatcher.BeginInvoke((ThreadStart)(() => dispatcherFrame.Continue = false), dispatcherPriority);
    Dispatcher.PushFrame(dispatcherFrame);
}
{% endhighlight %}

We can use that method like this:

{% highlight C# %}
if (showSplash)
{
    // pump until loaded
    PumpDispatcherUntilPriority(DispatcherPriority.Loaded);
 
    // start a timer, after which the splash can be closed
    var splashTimer = new DispatcherTimer
    {
        Interval = TimeSpan.FromSeconds(2)
    };
    splashTimer.Tick += (s, e) =>
    {
        splashTimer.Stop();
        CloseSplashScreen();
    };
    splashTimer.Start();
}
{% endhighlight %}

Great! Now we have a splash that closes two seconds after the application loads. You could also pump the dispatcher until some other criteria is met. Notice, however, that you need to be careful. If the user has sufficient time to open a dialog window within the application, this can preclude the closure of the splash screen (until the dialog is dismissed). That's because opening the dialog will result in a new dispatcher frame that pumps until the dialog is closed. That's precisely why I used a timer above instead of just pumping for a fixed period of time.

## Fixing the Activation Issue

Now that our splash closes some time after the application appears, another problem has surfaced. When our splash screen fades, it temporarily becomes activated. That's weird. And rather ugly.

After perusing [the source for `SplashScreen`](http://referencesource.microsoft.com/#WindowsBase/src/Base/System/Windows/SplashScreen.cs), I found the reason for this jarring activation. This code is in the fade logic:

{% highlight C# %}
// by default close gets called as soon as the first application window is created
// since it will have become the active window we need to steal back the active window
// status so that the fade out animation is visible. 
IntPtr prevHwnd = UnsafeNativeMethods.SetActiveWindow(new HandleRef(null, _hwnd));
{% endhighlight %}

The comment seems to suggest that the window is being activated only to ensure it is visible. Nasty.

So where does this discovery leave us? We must either put up with the activation of the fading splash screen, or forgo the fade logic in `SplashScreen` and write our own. I did the latter, and it looks like this:

{% highlight C# %}
private static void CloseSplashScreen(TimeSpan fadeDuration)
{
    var fadeDurationTicks = fadeDuration.Ticks;
    var remainingTicks = fadeDurationTicks;
    var lastCheckTicks = DateTime.UtcNow.Ticks;
    var dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal)
    {
        // 60fps
        Interval = TimeSpan.FromMilliseconds(1000 / 60d)
    };
    var opacity = 1d;
 
    dispatcherTimer.Tick += (s, e) =>
        {
            var tickChange = DateTime.UtcNow.Ticks - lastCheckTicks;
            lastCheckTicks = DateTime.UtcNow.Ticks;
            remainingTicks -= tickChange;
            remainingTicks = Math.Max(0, remainingTicks);
            opacity = (double)remainingTicks / fadeDurationTicks;
 
            if (remainingTicks == 0)
            {
                // finished fading
                splashScreen.Close(TimeSpan.Zero);
                dispatcherTimer.Stop();
                splashForegroundTimer.Stop();
                splashScreen = null;
                splashForegroundTimer = null;
            }
            else
            {
                // still fading
                blendFunction.SourceConstantAlpha = (byte)(255 * opacity);
                SafeNativeMethods.UpdateLayeredWindow(splashScreen.GetHandle(), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, ref blendFunction, SafeNativeMethods.ULW_ALPHA);
            }
        };
    dispatcherTimer.Start();
}
{% endhighlight %}

As you can see, I set up a timer that continuously fades the splash screen until it is no longer visible, at which point I close it. At no point do I activate the splash screen.

Notice the call to `splashScreen.GetHandle()`? That's where things have gotten a bit ugly. `SplashScreen` does not expose its window handle to us, so I wrote an extension method to obtain it via reflection:

{% highlight C# %}
internal static class SplashScreenExtensions
{
    public static IntPtr GetHandle(this SplashScreen @this)
    {
        return (IntPtr)typeof(SplashScreen).GetField("_hwnd", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(@this);
    }
}
{% endhighlight %}

Not ideal, but it works. We can now fade our splash screen ourselves without it being activated.

## Taming the Z-order

There's nothing more obnoxious that a splash screen that insists on covering other applications. OK, apart from [Ken Ham](http://en.wikipedia.org/wiki/Ken_Ham) that is. So when we show our splash screen, we should pass `false` for the `topmost` parameter (or don't specify the parameter at all, since `false` is the default value):

{% highlight C# %}
splashScreen.Show(autoClose: false, topMost: false);
{% endhighlight %}

However, doing so presents another issue. Other windows within our application can obstruct the splash screen. As we discovered in the last section, this is why WPF’s `SplashScreen` calls [`SetActiveWindow`](http://msdn.microsoft.com/en-gb/library/windows/desktop/ms646311(v=vs.85).aspx). But `SetActiveWindow`‘s primary purpose is to activate a window – bringing the window to the top of the stack is just a side-effect of that. We would like to bring our splash to the top of the window stack without activating it. Moreover, we need to ensure it remains at the top of the stack regardless of how many application windows happens to come (and maybe even go) during the time the splash is on-screen or fading away.

The first issue can be addressed by using [`SetWindowPos`](http://msdn.microsoft.com/en-gb/library/windows/desktop/ms633545(v=vs.85).aspx) instead of `SetActiveWindow`. It allows us to pull a window in our application to the top of the stack without activating it.

The second issue – ensuring our splash stays on top even as application windows come and go – is trickier. I considered many approaches, including setting the splash window to be a child of the application window (but what if there are multiple application windows?), detecting the activation of new windows so I can call `SetActiveWindow` again on the splash, and hooking into message queues.

Ultimately I settled on the simplest possible thing I could get to work:

{% highlight C# %}
splashForegroundTimer = new DispatcherTimer(DispatcherPriority.Normal);
splashForegroundTimer.Interval = TimeSpan.FromMilliseconds(10);
splashForegroundTimer.Tick += delegate
{
    SafeNativeMethods.SetWindowPos(splashScreen.GetHandle(), SafeNativeMethods.HWND_TOP, 0, 0, 0, 0, SafeNativeMethods.SetWindowPosFlags.SWP_NOMOVE | SafeNativeMethods.SetWindowPosFlags.SWP_NOSIZE | SafeNativeMethods.SetWindowPosFlags.SWP_NOACTIVATE);
};
splashForegroundTimer.Start();
{% endhighlight %}

I have a timer that brings the splash screen to the top of the z-order every 10ms (I didn’t experiment too much with this interval, so it may be possible to lengthen it). Notice that I use `SWP_NOACTIVATE` to ensure the splash isn't activated. Notice also that this isn't a perfect solution. There is still a 10ms (give or take) window wherein the splash may be underneath application windows. In practice, however, I have found this to work satisfactorily. If anyone has any other ideas on how to achieve this more cleanly and simply then I’d love to hear them.

## Conclusion

What we end up with when applying all the above changes is something rather more complicated than one might expect. Indeed, it makes me wonder whether I should forgo WPF’s `SplashScreen` class altogether and create my own. However, I have avoided doing so for a couple of reasons:

* `SplashScreen` has quite a bit of complexity in it.
* `SplashScreen` is in *WindowsBase.dll*, which is used by any WPF application, and is NGEN'd. Consequently, it's more likely to load faster than my own assembly.

I did, however, create my own wrapper around WPF's `SplashScreen` class. To confuse matters, I called mine `SplashScreen` too. Its API is very similar to that provided by WPF's `SplashScreen`.

[Here is a sample project]({{ page.assets }}SplashTest.zip) showing all these techniques combining to result in a splash screen I (and my users) can live with:

![Splash]({{ page.assets }}splash.png "Splash")