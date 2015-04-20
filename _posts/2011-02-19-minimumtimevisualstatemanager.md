---
title: MinimumTimeVisualStateManager
assets: /assets/2011-02-19-minimumtimevisualstatemanager/
tags: [ ".NET", "WPF", "Silverlight", "XAML" ]
---
A little while back I did some Silverlight work that was very focused on providing a fantastic user experience. As part of this work I developed a control that displayed a loading animation:

![Loading Animation]({{ page.assets }}loading_animation.gif "Loading Animation")

I then had views that would display this loading control whilst they're in a *Loading* state, and then hide it when they change to a *Loaded* state. That all worked fine. The problem was that sometimes - if the load operation completed quickly enough - the loading animation would briefly flash up on the screen and then disappear. Instead of improving the user experience, it was worsening it.

I could have applied a lengthy duration to the *Loading* -> *Loaded* transition, but that would apply indiscriminately to both fast loads, and slow loads. In other words, if the data happened to load slowly, users would have to wait for the slow load, and then again for the slower transition. Hardly compelling from a UX perspective.

What I really wanted to do was impose a minimum amount of time to spend in a particular state. That way I could say "the *Loading* state must be active for at least 1 second" rather than "the *Loading* state will be active for a minimum of 1 second". If the load operation took half a second, the loading animation would still display for a full second. If the load operation took two seconds, the loading animation would only display for that two second period (plus whatever transition time I specified).

Fortunately, the Visual State Manager infrastructure is extensible in that it allows custom a `VisualStateManager` instance to be associated with a given element. Such a custom manager might do something as simple as logging visual state changes, or something more complex. In my case, I wanted to enforce a minimum time for any state that desires one, which implies an attached property to set that minimum time and some logic to delay state changes when necessary.

Enter, the `MinimumTimeVisualStateManager`:

```C#
public class MinimumTimeVisualStateManager : VisualStateManager
{
    public static readonly DependencyProperty MinimumTimeProperty = DependencyProperty.RegisterAttached("MinimumTime",
        typeof(TimeSpan),
        typeof(MinimumTimeVisualStateManager),
        new PropertyMetadata(TimeSpan.Zero));
 
    private static readonly DependencyProperty StateChangeMinimumTimeProperty = DependencyProperty.RegisterAttached("StateChangeMinimumTime",
        typeof(DateTime),
        typeof(MinimumTimeVisualStateManager),
        new PropertyMetadata(DateTime.MinValue));
 
    public static TimeSpan GetMinimumTime(VisualState visualState)
    {
        if (visualState == null)
        {
            throw new ArgumentNullException("visualState");
        }
 
        return (TimeSpan)visualState.GetValue(MinimumTimeProperty);
    }
 
    public static void SetMinimumTime(VisualState visualState, TimeSpan minimumTime)
    {
        if (visualState == null)
        {
            throw new ArgumentNullException("visualState");
        }
 
        visualState.SetValue(MinimumTimeProperty, minimumTime);
    }
 
    private static DateTime GetStateChangeMinimumTime(DependencyObject dependencyObject)
    {
        Debug.Assert(dependencyObject != null);
        return (DateTime)dependencyObject.GetValue(StateChangeMinimumTimeProperty);
    }
 
    private static void SetStateChangeMinimumTime(DependencyObject dependencyObject, DateTime stateChangeMinimumTime)
    {
        Debug.Assert(dependencyObject != null);
        dependencyObject.SetValue(StateChangeMinimumTimeProperty, stateChangeMinimumTime);
    }
 
    protected override bool GoToStateCore(FrameworkElement control, FrameworkElement stateGroupsRoot, string stateName, VisualStateGroup group, VisualState state, bool useTransitions)
    {
        Debug.Assert(group != null && state != null && stateGroupsRoot != null, "Group, state, or stateGroupsRoot is null for state name '" + stateName + "'. Be sure you've declared the state in the XAML.");
        var minimumTimeToStateChange = GetStateChangeMinimumTime(stateGroupsRoot);
 
        if (DateTime.UtcNow < minimumTimeToStateChange)
        {
            // can't transition yet so reschedule for later
            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Interval = minimumTimeToStateChange - DateTime.UtcNow;
            dispatcherTimer.Tick += delegate
            {
                dispatcherTimer.Stop();
                this.DoStateChange(control, stateGroupsRoot, stateName, group, state, useTransitions);
            };
            dispatcherTimer.Start();
 
            return false;
        }
 
        return this.DoStateChange(control, stateGroupsRoot, stateName, group, state, useTransitions);
    }
 
    private bool DoStateChange(FrameworkElement control, FrameworkElement stateGroupsRoot, string stateName, VisualStateGroup group, VisualState state, bool useTransitions)
    {
        var succeeded = base.GoToStateCore(control, stateGroupsRoot, stateName, group, state, useTransitions);
 
        if (succeeded)
        {
            SetStateChangeMinimumTime(stateGroupsRoot, DateTime.MinValue);
            var minimumTimeInState = GetMinimumTime(state);
 
            if (minimumTimeInState > TimeSpan.Zero)
            {
                SetStateChangeMinimumTime(stateGroupsRoot, DateTime.UtcNow + minimumTimeInState);
            }
        }
 
        return succeeded;
    }
}
```

By way of explanation, the `MinimumTimeVisualStateManager` uses an attached property called `MinimumTime` which can be set on `VisualState` instances. When set, any calls to `GoToStateCore` will ensure that the required minimum time has been surpassed. If so, the state change succeeds as per normal. If not, a `DispatcherTimer` is used to switch states at an appropriate point later in time.

With this custom visual state manager, we can impose minimum times for states like this:

```XML
<Grid x:Name="root">
    <VisualStateManager.CustomVisualStateManager>
        <kb:MinimumTimeVisualStateManager/>
    </VisualStateManager.CustomVisualStateManager>
     
    <VisualStateManager.VisualStateGroups>
        <VisualStateGroup Name="LoadStates">
            <VisualState Name="Loading" kb:MinimumTimeVisualStateManager.MinimumTime="00:00:01">
            ...
```

In the above example, we ensure that the *Loading* state is active for at least 1 second.

By the way, this works equally well with Silverlight / WPF. You can [download a sample WPF project]({{ page.assets }}MinimumTimeVisualStateManagerExample.zip) showing this in action.