---
title: Xamarin Link Error
assets: /assets/2015-03-14-xamarin-link-error/
tags: [ "C#", ".NET", "Xamarin" ]
---
If you've got a Xamarin project that has linking enabled, it's likely you've modified your project properties to pass an additional `mtouch` argument as follows:

    --xml=${ProjectDir}/linker.xml

Recently (maybe due to an update, or maybe due to switching from Xamarin Studio to Visual Studio) this starting balking for me with an error like this:

    Error Extra linker definitions file '[path]' could not be located.

If you get the same, be sure to modify the properties of your *linker.xml* file (or whatever you called yours) such that **Copy to Output Directory** is set to **Copy if newer**.