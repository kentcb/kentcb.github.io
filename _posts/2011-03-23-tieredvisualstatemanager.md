---
title: TieredVisualStateMangager
assets: /assets/2011-03-23-tieredvisualstatemanager/
tags: [ ".NET", "WPF", "XAML" ]
---
A [couple of posts ago]({% post_url 2011-02-19-minimumtimevisualstatemanager %}), I discussed a custom visual state manager that allows you to impose a minimal amount of time for which a state must be active. In this post I'm going to push the visual state management infrastructure even further out of its comfort zone.

Imagine you've just finished developing a kick-ass WPF application that has extracted "ooooh"s and "aaaah"s from all the stakeholders. You're at the pub when your boss calls you and asks why the application keeps hanging on his machine. "Hanging?", you ask, reluctantly putting down the celebratory beer. "Yes, it stops and starts like a handsaw.", he replies. "Oh, you mean stuttering." "That's what I said - stuttering."

Intrigued, you head back into the office. Before your boss even fires up the application you notice the turbo button on his PC and your head drops into your hands. Predictably, your application takes a full minute to fire up and then renders at a frame rate of three Walt Disney animators.

"It's your machine.", you say dejectedly. "I don't care what it is - fix it. Today!"

You drag your feet all the way back to your desk, pondering the state of the job market in your area. You sit down at your desk and crack open your solution, the solution that was your pride and joy a mere hour ago. You don't bother firing up a profiler because you had already profiled and tweaked the performance of the application prior to shipping - you know there's nothing significant left to squeeze out of it.

At this point most of us would simply strip out animations, transitions, effects, and other non-critical niceties that are placing a strain on the frame rate. This has the advantage of getting your boss off your back (and you back at the pub), but the disadvantage of lowering the experience for users who have hardware dating post 1990. Not to mention the fact that you've likely had a UX designer put in significant amounts of thought and effort into producing the experience.

I anticipate this exact problem with the application I'm currently developing - there is a varied user base with varied hardware capabilities. OK, none have a turbo button that I'm aware of, but it wouldn't surprise me.

I decided to put some effort in to come up with a mechanism by which we could tier the experience in our application. That is, a mechanism by which the entire skin of the application can be scaled up or down based on some criteria. Whilst you could achieve this feat by maintaining multiple resource dictionaries - one for each tier in your scale - this results in a lot of duplication of work, since many tiers will include aspects present in other tiers. Another approach might involve some kind of global bindable property that determines the active tier, and then expecting XAML to trigger changes off that property. Again, this would work but would result in a mess of triggers in your templates and styles.

A far better approach, I think, is to create a custom visual state manager. Doing so can allow us to avoid duplication of work, and can keep our XAML relatively clean. This visual state manager, which I've called TieredVisualStateManager, must provide the following:

* a global property tracking the tier that is currently active for the application
* the ability to assign visual states and transitions to specific tiers
* the ability to aggregate visual states and transitions together according to the tier to which they are assigned, and the active tier for the application

You can find a sample attached. In this sample, I've re-templated a `CheckBox` control as follows:

![Example]({{ page.assets }}example.gif "Example")

* low tier: very simple graphic, no animations
* medium tier: more appealing graphics, animates checking/unchecking by fading
* high tier: same graphics as medium, same checking/unchecking transitions, but also adds another animation to shake the CheckBox up and down (when checking) or left and right (when unchecking). In addition, there is a non-stop glowing animation.

For the full control template, check out the [attached sample]({{ page.assets }}TieredExperience.zip). However, this outline gives you a taste for how things work:

```xml
<local:TieredVisualStateManager x:Key="TieredVisualStateManager"/>
     
<Style TargetType="CheckBox">
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="CheckBox">
                <BulletDecorator VisualStateManager.CustomVisualStateManager="{StaticResource TieredVisualStateManager}">
                    <local:TieredVisualStateManager.TieredVisualStateGroups>
                        <local:TieredVisualStateGroupCollection>
                            <!--
                            CommonStates
                            -->
                            <local:TieredVisualStateGroup Name="CommonStates" Tier="Low">
                                <local:TieredVisualState Name="Normal"/>
                            </local:TieredVisualStateGroup>
                            <local:TieredVisualStateGroup Name="CommonStates" Tier="Medium">
                                <local:TieredVisualState Name="Normal">
                                    <!-- fancy brushes applied here -->
                                </local:TieredVisualState>
                            </local:TieredVisualStateGroup>
                                 
                            <local:TieredVisualStateGroup Name="CommonStates" Tier="High">
                                <local:TieredVisualState Name="Normal">
                                    <!-- glowing animation applied here -->
                                </local:TieredVisualState>
                            </local:TieredVisualStateGroup>
                            <!--
                            CheckStates
                            -->
                            <local:TieredVisualStateGroup Name="CheckStates" Tier="Low">
                                <local:TieredVisualState Name="Unchecked"/>
                                <local:TieredVisualState Name="Checked">
                                    <!-- display check mark here -->
                                </local:TieredVisualState>
                            </local:TieredVisualStateGroup>
                            <local:TieredVisualStateGroup Name="CheckStates" Tier="Medium">
                                <local:TieredVisualStateGroup.Transitions>
                                    <VisualTransition From="Unchecked" To="Checked">
                                        <!-- fade check mark in here -->
                                    </VisualTransition>
                                    <VisualTransition From="Checked" To="Unchecked">
                                        <!-- fade check mark out here -->
                                    </VisualTransition>
                                </local:TieredVisualStateGroup.Transitions>
                            </local:TieredVisualStateGroup>
                                 
                            <local:TieredVisualStateGroup Name="CheckStates" Tier="High">
                                <VisualStateGroup.Transitions>
                                    <VisualTransition From="Unchecked" To="Checked">
                                        <!-- shake the check box up and down here -->
                                    </VisualTransition>
                                    <VisualTransition From="Checked" To="Unchecked">
                                        <!-- shake the check box left and right here -->
                                    </VisualTransition>
                                </VisualStateGroup.Transitions>
                            </local:TieredVisualStateGroup>
                        </local:TieredVisualStateGroupCollection>
                    </local:TieredVisualStateManager.TieredVisualStateGroups>
                     
                    ...
                </BulletDecorator>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

Importantly, notice how I have not repeated anything. For example, the transitions to fade the check mark in and out are defined in the medium tier, but they will also be present if the active tier is high. Similarly, the logic to turn on the check mark is defined in the low tier CheckStates, but is inherited by higher tiers. This aggregation is controlled by the `TieredVisualStateManager.AggregateTiers` property, which is true by default. Setting it to `false` may be useful in some scenarios, but it does mean you may need to repeat yourself.

When used carefully, the `TieredVisualStateManager` allows you to segregate your application experience so that users with rubbish hardware do not impose on the experience of users with decent hardware. Or it may be that some users have great machines, but just dislike animations, and would rather save that little bit of time by turning them off.

But the `TieredVisualStateManager` is not just about animations. You can control almost anything through this mechanism: turn effects on for high experiences, turn semi-transparency off for low experiences, remove parts of the visual tree for low experiences, etc. You can even change the tier structure to suit your needs - I just defined `Low`, `Medium`, and `High` because it makes sense for my scenario. Just make sure you define them in order from lowest to highest experience because the internal logic of `TieredVisualStateManager` depends on that order<sup>†</sup>.

In order to actually set the active tier, you might use configuration, an options dialog, diagnostics of the user's machine, or some combination of both. I'm planning on just shipping with a high experience on, and exposing the setting somewhere in an options dialog.

"What about the Blend experience?", I hear you ask. Well, I personally don't use Blend the way in which it is purported to be used – I just don't think it works that well. I prefer using Blend for discrete tasks that are normally disconnected from my main solution. If I generate XAML with Blend, I always copy it manually and vet it for verbosity and other little pet hates of mine. Anyway, the fact is that we’ve taken the VSM sufficiently far enough out of its comfort zone that getting it to work in Blend is likely an impossibility. So I didn't put any effort into making the `TieredVisualStateManager` blendable. My plan is to obtain the high experience assets from the UX guy, and to "manually" apply tiering to them.

It’s an unfortunate story for Silverlight, too. For whatever reason, `VisualStateGroup` and `VisualState` are both `sealed` in Silverlight, which obviously breaks the entire approach to this problem. Perhaps it’s possible to hack around it, but until I need it in Silverlight I don’t plan on putting myself through that pain.

[Download the sample project](({{ page.assets }}TieredExperience.zip).

<sup>†</sup> Yes, you could abstract the definition of tiers out into a property on `TieredVisualStateManager`, thus yielding even more flexibility.