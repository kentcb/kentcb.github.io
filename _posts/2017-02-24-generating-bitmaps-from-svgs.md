---
title: Generating%20Bitmaps%20from%20SVGs
assets: /assets/2017-02-24-generating-bitmaps-from-svgs/
tags: [ "xamarin", "mobile", "iOS", "Android", "UWP" ".net", "C#" ]
---
One of the more tedious and thankless tasks in mobile development is the management of bitmaps. Each platform has its own naming convention and its own set of supported scales. If you want your bitmaps to appear crisp on a wide array of devices, you'll soon be managing a dozen or so copies of each bitmap. This can quickly lead to frustration and mistakes that are difficult to catch (and it may well be your customers who find the problem).

Some time ago, after struggling with this in a couple of projects, I set about concocting a solution. I wrote a C# script that automatically exports SVGs to each of the desired sizes, for each of the necessary platforms. With great flair of imagination, I named my script [_Generate.csx_](https://github.com/code-haeroes-pty-ltd/Generate).

Recently, Jonathan Dick of Xamarin released a tool called [Resizetizer](https://github.com/Redth/Resizetizer). It has similar goals to my script, but approaches the problem in a very different manner. Xamarin PM, Pierce Boggan, prompted me to write up some details on my solution, and this post is the result. Herein, I will compare my solution to Jonathan's.

The first and most obvious difference between the two tools is the build integration. Resizetizer is intended to integrate directly into your build process, thus it will run every time you build (I haven't checked, but presumably it will intelligently ignore images that do not require re-generation).

_Generate.csx_, in contrast, is designed to be run outside of the normal developnment process. It is only run on an as-need basis, and it might even be a designer running it. It is intelligent enough to skip the generation of bitmaps when the source SVG was modified earlier than the destination bitmap. Running the script is as simple as:

```
csi .\Generate.csx
```

If required, there is a `-force` parameter to ignore timestamps, thereby forcing the regeneration of images whether it was required or not.

The next big difference between the tools is how you configure them. Resizetizer, being a compiled tool, has an external configuration file (YAML). For every image you require, you must list it under the assets section under each platform you support. You must also supply a base size for that image.

_Generate.csx_ is, in my opinion, far easier in this regard. For each image _type_ you need to support, you define a method that forwards onto `ExportItem`:

```C#
private static void ExportToolBarItem(string name) =>
    ExportItem(name, "toolbar item", 24, 22, 20);
```

It is `ExportItem` that does the real work based on some simple parameters you supply it. Now, whenever you need a new toolbar item image, you simply add it to a list at the start of the script:

```C#
new[]
    {
        "hamburger",
        "foo",
        "bar"
    }
    .ToList()
    .ForEach(ExportToolBarItem);
```

If you ever need to change the size of all your toolbar icons, you have only one point in the script to modify (the `ExportToolBarItem` method). You don't have to spelunk through a long YAML file, figure out which images are toolbar icons, and update each of their base sizes.

Resizetizer supports the resizing of non-vector sources, such as PNGs. _Generate.csx_ does not, partly because I've had no need for it, and partly because I wouldn't want to encourage it. Resizing a bitmap can introduce artifacts that will reduce the professional feel to your images. That said, it would be simple to modify the script to do so if you so desire.

The means by which bitmaps are generated also differs between the tools. Resizetizer uses SkiaSharp whereas _Generate.csx_ simply forwards to an external tool (Inkscape). Naturally, if you have another tool that you'd prefer to use when generating images you can do so.

_Generate.csx_ will also optimize generated images using [PNGOut](https://en.wikipedia.org/wiki/PNGOUT). Again, you can swap this out for something else if desired.

Resizetizer does have an `optimize` flag in its configuration, but it currently does nothing. Instead, you will need to utilize the provided hooks to run custom external tools against each generated image.

There are, of course, other differences between the tools. However, I believe I've covered the most important ones here. One thing that _is_ exactly the same is that both tools are distributed under the MIT license.

If you want to try out _Generate.csx_ as a solution, here's some things to be aware of:

1. The script assumes it lives in a directory alongside an _Src_ directory. We use an _Art_ directory, but whatever takes your fancy. Of course, change the script if you want something completely different.
2. Place your SVG files alongside the script.
3. Add the name of the file (without the extension) to the appropriate section of the script to have images generated from it.

Enjoy!

[_Generate.csx_](https://github.com/code-haeroes-pty-ltd/Generate)

[Resizetizer](https://github.com/Redth/Resizetizer)