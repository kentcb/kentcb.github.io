---
title: How Codemagic Helped get the Job Done
assets: /assets/2019-06-20-how-codemagic-helped-get-the-job-done/
tags: [ "Flutter", "Codemagic", "CI", "Bitrise" ]
---

This week I published my mobile app, _Job Done_, to the app stores (see [here for iOS](https://apps.apple.com/au/app/jobdone/id1463513891) and [here for Android](https://play.google.com/store/apps/details?id=au.com.crosslinkbs.jobdone); currently available in AU, NZ, and EU). It's a super-useful tool for tradespeople to reduce the amount of time they spend on quoting, invoicing, and generally managing their business. If you know any tradies (and who doesn't?) please ask them to try it out and provide feedback.

Towards the end of February 2019, some 6 months into the development of _Job Done_, I migrated my CI from [Bitrise](https://www.bitrise.io/) to [Codemagic](https://codemagic.io/). Now, Bitrise is a _fantastic_ tool and I highly recommend checking it out to see if it aligns with your requirements. In fact, I continued to use it in my day job. My primary motivation for moving away from it for my personal project was cost. The available Bitrise pricing tiers are such that I am required to spend ~USD40 per month, even though I was working on the app only here and there and needed maybe three or four builds max _per month_. I found ~USD10 per build hard to swallow! Whilst the Bitrise "Hobby" tier is free, it only allows up to 10 minutes per build, which is barely enough to pull dependencies down for an iOS application, let alone build it!

Speaking of iOS builds, a secondary motivation was an issue I was experiencing where iOS builds were frequently timing out my 45 minute allowance. It was unclear from the log why this might be, and the Bitrise support team (who are brilliant and super helpful, just quietly) were unable to figure out why either. More on this shortly.

Serrendipitously, there was a new CI player on the scene: Codemagic. Their infancy meant they didn't even have a pricing model established at the time, and I could use their service for "free". I also sought, and obtained, assurances from Ann Sepp (Partnership Manager @ Codemagic) - that I wouldn't have free pricing pulled from underneath me at a later date:

![Codemagic Pricing Discussion]({{ page.assets }}codemagic_pricing.png "Codemagic Pricing Discussion")

I therefore decided to migrate my CI to Codemagic (initially in a separate branch, of course) to see if they were a viable alternative. This blog post is an open and honest catalog of the good and the bad experiences I had performing this migration. It includes some workarounds for some of the problems you may yourself encounter when using Codemagic.

# Build Timeouts

First of all, I was distraught to discover that despite moving to Codemagic, I continued to see unpredictable timeouts when building for iOS. In a way, this was a good clue because it was now evident that the problem was not specific to Bitrise and must therefore be lower in the stack.

I burnt a _lot_ of time on this problem and collected quite a bit of data (thankfully the pricing was still free throughout most of this), some of which I put together more formally:

![Build Timeouts]({{ page.assets }}build_data.png "Build Timeouts")

To cut a very long story short, the Flutter team ended up improving the code that orchestrates third-party tooling during builds (for example, the tools that are executed when you run `flutter build ios`). The main change was to perform process execution asynchronously, per [this PR](https://github.com/flutter/flutter/pull/29048). This certainly helped a lot with many timeouts.

Unfortunately, I do still get occasional timeouts, and that's even though my build time limit has been bumped to 60 minutes from the standard 45. I just experience timeouts much less frequently than before. As I understand it, this is no longer happening due to Flutter waiting around for processes that have already completed, but because the build VMs used by Codemagic are using a virtualization technology that is not performing very well. The Codemagic team are looking to make large improvements in this area, so I'm eagerly watching this space. iOS builds were also very slow on Bitrise, so I'd imagine both companies are moving towards improving the situation.

# Flexibility and Control

To me, the biggest difference between Bitrise and Codemagic is in the philosophy of the platforms and the impact that has on their flexibility. Bitrise targets mobile apps in a broad sense whereas Codemagic hones in on the Flutter ecosystem. As a consequence of their broad scope, Bitrise is incredibly flexible and can readily be leveraged even outside the mobile space (and often is). This makes Codemagic a natural choice if you're building a Flutter app with no supporting infrastructure, but things get complicated if you also need to build a back-end, or if all your mobile apps aren't built with Flutter.

_Job Done_ is indeed a Flutter app, but it also requires back-end components. It relies on both Firebase and a Google App Engine application. These parts of the system reside in separate repositories and it made sense for me to want to have those repositories build via Codemagic as well (rather than having to manage two CI solutions to build the one system). This was more painful than it should be.

As it stands, Codemagic assumes you're building a Flutter application. Indeed, if it doesn't find a `pubspec.yaml` file with a flutter SDK dependency within, it will fail to build. This meant that in order to build something as simple as the platform-agnostic DTOs that are shared between my app and the back-end, I had to add this hack into my post-clone script:

```bash
#!/usr/bin/env bash
# fail if any commands fails
set -e
# debug log
set -x

# HACK. See https://receptive.io/app/#/case/89995
echo "  flutter:" >> ./pubspec.yaml
echo "    sdk: flutter" >> ./pubspec.yaml
```

**UPDATE 11th January, 2020:** Codemagic no longer requires this hack to build a non-Flutter project. But note that your scripts will only run if the corresponding step runs. You can see which steps are executed in the log.

As a rule, I try to keep as much of my automation in my own code base rather than embedding it into the configuration of a third party tool like Bitrise or Codemagic. This is because it means I have a more complete history of its evolution alongside the code, and because it is far simpler to shift to another tool altogether (such as moving from Bitrise to Codemagic, or vice-versa). For that reason, I use [grinder](https://pub.dev/packages/grinder) scripts to do a bunch of auxiliary work around my builds such as:

* consistent versioning and release notes
* copying in the correct credentials for the target environment
* filling out `build.gradle`, `Info.plist`, and other metadata
* uploading dSYMs to crash reporting back-ends
* tagging git

All this is to say, I need to be able to execute some Dart code (my grinder script) before my app is built. Even this turned out to be something of a hassle on Codemagic because Flutter's Dart is not automatically in your path. I therefore execute grinder as follows:

```bash
#!/usr/bin/env bash
# fail if any commands fails
set -e
# debug log
set -x

export PATH=$HOME/programs/flutter/bin/cache/dart-sdk/bin:$HOME/programs/flutter/.pub-cache/bin:$PATH

flutter packages pub global activate grinder
grind build
```

OK, it's not a big thing and once you know it, you know it. But it is a little more added friction and it does seem kind of odd to me that a tool for building Flutter apps does not make any concerted effort to allow you, at the very least, to build supporting Dart code.

In addition to the above, you will also need to disable tests:

![Disable Tests]({{ page.assets }}disable_tests.png "Disable Tests")

And, counterintuitively, set the build to "Run tests only":

![Disable Build]({{ page.assets }}disable_build.png "Disable Build")

Publishing can then be ignored, since no platform builds are being executed.

Of course, the above assumes you're doing your building via a grinder script, otherwise your workflow wouldn't be doing anything of value.

The screenshots for _Job Done_ are produced automatically across a range of devices and operating systems. I've also spent a bit of time trying to implement a `screenshots` workflow in Codemagic that simply executes this automated screenshot production. However, I've so far been unsuccessful. I ran into issues trying to install and configure the required emulators and had to park this problem until I have time later (for now, I'm happy just to run the automated script on my Mac Mini and copy the screenshots to the app stores). This again felt like I was pushing up against the limits of Codemagic's design, and I rather suspect I'd have a far easier time of it on Bitrise.

It's worth noting that, when building with Bitrise, I do _everything_ via my script, including the execution of the Flutter tooling itself. This gives me ultimate control and ensures my investment in CI is far more self-contained and portable. Codemagic requires you to jump through a few hoops to achieve this, and it is a little hacky. In theory, nothing prevents you from installing tooling completely unrelated to the Flutter ecosystem (indeed, I have to install npm for my Firebase build), but you really feel like you're swimming upstream when you do this. Bitrise, on the other hand, can be distilled down to the point where all it does is instigate a build and immediately pass control over to you for _everything_. Codemagic is not yet like this, and it may never be. This is not necessarily a bad thing, but I do feel it requires greater flexibility than it currently affords.

# Configuration as Code

Configuring Codemagic involves clicking through an attractive UI. Your configuration is not manifest in any kind of textual format, so it cannot live alongside your code, nor is a history of your changes kept. This is fine for small projects, but it quickly becomes a hassle when you have several projects, each with several workflows. _Job Done_ required 5 projects, with 12 workflows between them. There were times when I really felt the pain of having to manually copy a change across many workflows.

Bitrise allows you to configure your workflows via a GUI or via YAML. Critically, that YAML can be placed _in your repository_, giving you all the benefits thereof: versioning, change review, history etc. It makes it far easier to make configuration changes and much safer to experiment (because you can always just roll back or delete your branch). The only problems I've really had with it is that [it's YAML](https://noyaml.com/) and its design sometimes makes composition difficult or impossible without having to resort to a lower-level abstraction (a Bitrise "step", which is written in Go).

There's good news in this space for Codemagic users, however. This was causing me enough pain that I created [a support issue for it](https://receptive.io/app/#/case/90085) and it now being worked on. Special thanks to Martin Jeret (Head of Business Development @ Codemagic) for the conversations on this issue and others. My hope is that it is even better than the Bitrise experience.

# Support (Receptive)

Codemagic use something called [Receptive](https://www.receptive.io/) for "support". I'd never heard of it and I don't want to ruffle any feathers here, but the truth is that it's _terrible_. For starters, even getting access is incredibly clunky. If you clicked on the support link I had at the end of the last section you may have seen something like this:

![Receptive Log In]({{ page.assets }}receptive_auth0.png "Receptive Log In")

The thing is, Codemagic automatically propagates your identity to Receptive, so you have to open Receptive via Codemagic (the blue button at the bottom-left). This makes sharing Receptive links somewhat dubious at best. Recipients of the link have to understand they need to login to Codemagic first, open Receptive, and _then_ open the link. But even then, I'm often greeted by this:

![Receptive Log In Again]({{ page.assets }}receptive_auth1.png "Receptive Log In Again")

In such cases, I normally just have to try again until it works.

Once you finally _do_ get on to Receptive, you wonder why you bothered. It's more or less a flat list of unorderable, unfilterable, uncurated suggestions. You can't even easily pull up a list of _your_ suggestions so that you can track their progress. There is very little community discussion going on, presumably because it's so cumbersome to gain access to in the first place. There is also no ability to report bugs, which is very odd. Basically, Receptive appears to be little more than a wishlist of features with a status. In my opinion, Receptive should be replaced with a GitHub repository because it is actively harming the Codemagic community.

There is also a Slack channel for Codemagic, as you may have inferred from some of the screenshots above. However, in my experience Slack is not a good long-term support solution. It should be used to instigate and for ephemeral conversation. Codemagic needs a more permanent, web-searchable, and easily accessible record of support interactions and GitHub issues would provide that. As far as I'm aware, there is no other support channel. Again, I think this is harmful to the product, since support via Slack simply does not work. Conversations are easily lost and go unanswered. And when problems _are_ resolved, the solution becomes harder and harder to find, causing frustration and wasted time for those who have to re-answer the same question.

Bitrise also have a Slack channel and they rightly push relevant conversation into [their Discuss](https://discuss.bitrise.io/). Discuss is way better than Receptive, but my preference would still be GitHub for a developer product.

# Diagnostics

When you build with Bitrise, you get a single log file as a result. Parts of it are obfuscated to prevent leaking secrets, but other than that it's basically everything you could want to know about your build.

As of today, Codemagic is different. For starters, there is no unified log either for download or for display. Instead, the log for each step is displayed independently in the UI, and each step needs to be expanded first. You end up with quite a clunky experience because you have scroll viewers within a scroll viewer and it's very easy to get lost as you attempt to move around. Searching across your entire log is nigh on impossible.

But it gets worse.

I found out (the hard way, of course) that the logs you see in the Codemagic UI are not even the complete story. There are "internal" logs that only the Codemagic team currently have access to. This lack of visibility put me through quite a bit of pain on several occasions.

Another issue arises when you want to have access to the VM's file system. I had to resort to sticking `grep` invocations in my scripts in order to track down where certain files actually were on the filesystem. Bitrise allows you to download caches or even remotely access the VM, which makes such situations far easier.

From my discussion with Martin, I understand the Codemagic team are looking to address some of these shortcomings, so watch this space.

# Maturity

Due to Codemagic being a much younger product than Bitrise, I inevitably ran into some issues as a result. For a long time, documentation was completely lacking. Thankfully, that has since [been rectified](https://docs.codemagic.io/).

I also lost quite a bit of time on an issue where the name of the IPA being produced by the build was incorrect because it had the wrong environment name in it (even though the binary itself was correct). As per the Slack conversation below, this turned out to be because Codemagic was reading and caching cloned files way earlier than it should have been. This was one of those cases when a full, downloadable log would probably have saved me a lot of time.

![IPA Name Problem]({{ page.assets }}ipa_name_problem0.png "IPA Name Problem")![IPA Name Problem]({{ page.assets }}ipa_name_problem1.png "IPA Name Problem")

There have been a few other little issues along the way, but the Codemagic team have been quick to rectify once the problem was clear. Overall, I've been very impressed with the stability of Codemagic.

# Pricing Model

I've already talked a little about the pricing model of the two services, but I wanted to make the further point that Codemagic have now introduced a pricing model (which, you'll recall, didn't exist when I first migrated). It's a very reasonable model where you get 500 build minutes for free each month and only pay if you need to exceed this limit. Right now, that gets me around 10 builds of _Job Done_, which will generally be more than enough (I had to disable auto-triggering of dev builds though - I only trigger automatically for release builds).

What's particularly important is that you can request an increase to your timeout. This is unlike Bitrise, which has set timeouts per pricing tier. As I mentioned earlier, my build timeout is 60 minutes rather than 45 because most of my app builds take around 50-55 minutes (the non-Flutter projects build much more quickly because there's obviously no iOS involved). I expect I'll be able to drop this timeout quite a bit once the iOS VM infrastructure is sorted out.

Basically, the Codemagic pricing model is _perfect_ for me because I only build infrequently. On those rare occasions where I need a lot of builds in a single month, I will happily drop USD19 on more build time. Indeed, I had to do this in my final push to get _Job Done_ out the door.

# Source Model

Bitrise is fully open source. You can pay to have a hosted version, or you can run it on your own hardware. Codemagic is closed source.

Generally speaking, I'm a big fan of my tools being open source. Being able to just peek under the hood when something goes wrong (or when you're just curious) is incredibly empowering. That said, I'm surprised by the fact that I've _never_ had to look at the Bitrise source code (although I have benefited from looking at some of the step implementations). Counterintuitively, Codemagic being open source would have helped me a lot more than Bitrise being open source ever did. I suspect that's just because Codemagic lacks maturity so there was more impetus for me to want to delve into its inner workings. So for me, I'm not terribly fussed about this because I think in the long run it will be far less an issue.

# UX

No disrespect to the Bitrise team (or to anyone for that matter - the people I've interacted with on both of these projects are awesome), but I've always found their UI a little hard to work with. Thankfully, you don't have to do that much because of their configuration as code, but it is a sore point all the same. I know they did a UX revamp a while back, and maybe they'll be doing another with their newly-secured [20 million](https://blog.bitrise.io/series-b-funding) (!), but it still doesn't gel with me. Too often I feel momentarily lost or confused in their UI, even though I'm an old hand at it.

Codemagic does better in this regard, though there are still pain points, such as the issue with logs I mentioned earlier. And of course, you have to spend a lot more time in the Codemagic UI than you do Bitrise.

# Summary

If you're doing work in the Flutter space, you should check out Codemagic as a CI solution. It has a ways to go before it competes with the flexibility and maturity of Bitrise, but it also has a number of advantages such as its pricing model, UX, and focus on Flutter (which cuts both ways, to be sure).

To reiterate, the Codemagic team are already putting effort into resolving some of the issues above, and my hope is that this post encourages further effort. Codemagic is a fantastically strong product already, and I'm looking forward to seeing it evolve further.