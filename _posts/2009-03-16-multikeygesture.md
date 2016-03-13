---
title: MultiKeyGesture
assets: /assets/2009-03-16-multikeygesture/
tags: [ ".NET", "WPF", "XAML" ]
---

A colleague recently asked whether it was possible to invoke a WPF Command when the user presses a sequence of keys. As an example, consider the Format Document command in Visual Studio, which is generally mapped to **Ctrl+K, D**.

![Menu]({{ page.assets }}menu.png "Menu")

In WPF, a Command can be invoked via "input gestures" in a couple of different ways:

1. When creating a `RoutedCommand`, you can add any number of `InputGesture` objects to the `InputGestures` collection. Adding an `InputGesture` to this collection ensures that the `Command` will be executed (if it can be) when that gesture is performed by the user.
2. Via the `InputBindings` collection on a `UIElement`. Adding an `InputBinding` to this collection ensures that a certain `InputGesture` will execute a certain `Command` against that `UIElement`.

WPF's built-in `KeyGesture` class is an `InputGesture` subclass that recognises a gesture based on keyboard input. The problem is, its definition of "gesture" is a single keypress. What I'd like to do is treat multiple keypresses as a single gesture, and `KeyGesture` does not support that.

Enter my `MultiKeyGesture` class. At first I thought I'd subclass `InputGesture` rather than `KeyGesture`, since the `KeyGesture` constructors reflect the single-key assumption. However, I found I had little choice but to subclass `KeyGesture` because the built-in WPF `MenuItem` control treats it as a special case in determining the text to display for the shortcut string. I could manually set the `InputGestureText` of the `MenuItem`, but I just couldn't be bothered for the purposes of this post.

Never mind though, because I can simply pass in `Key.None` to the `KeyGesture` constructor and override its matching logic:

```csharp
public MultiKeyGesture(IEnumerable<Key> keys, ModifierKeys modifiers, string displayString)
    : base(Key.None, modifiers, displayString)
{
    _keys = new List<Key>(keys); 
 
    if (_keys.Count == 0)
    {
        throw new ArgumentException("At least one key must be specified.", "keys");
    }
} 
 
public override bool Matches(object targetElement, InputEventArgs inputEventArgs)
{
    ...
}I
```

In other words, I'm only inheriting from `KeyGesture` so that our particular gesture implementation gels with other WPF classes that make annoying assumptions and break the Liskov substitution principle.

Notice from the constructor that the `MultiKeyGesture` class takes an enumeration of `Key`s. In order for a match to occur, the user must type in all of those keys (in order, of course). Generally, you would have a maximum of two keystrokes, but the sky really is the limit here. You could make the user type in **Ctrl+A, p, r, i, l, O, n, e, i, l, l, i, s, h, a, w, t** before your super-secret command is executed. You'd be pretty childish if you did that, but you could.

![April O'neil]({{ page.assets }}april.png "April")

The `Matches()` method is where all the magic happens. It is this method that determines whether the user input matches the keys in the `Keys` collection. It does this with a mini state machine. As input is received, one of three things can occur:

1. No match. The user entered the wrong key or paused too long between keystrokes. The index into the `Keys` collection is reset back to zero and the method returns `false`.
2. Partial match. The user entered a matching key but not all keys have been entered yet. The index into the `Keys` collection is incremented and the method returns `false` (but marks the `RoutedEvent` as handled).
3. Full match. The user entered the final matching key in the `Keys` collection. The index into the `Keys` collection is reset back to zero and the method returns `true`.

That's it really. All the usual disclaimers apply about blog-quality code, but it does at least show that the scenario is possible in WPF.

If you look at the code, you'll notice a couple of other classes:

* `MultiKeyBinding`
* `MultiKeyGestureConverter`

These aren't strictly necessary unless you want `UIElement.InputBindings` and XAML integration respectively. `MultiKeyBinding` is used to map a `MultiKeyGesture` to a particular `Command` for a `UIElement` (in XAML in this case, but could also be in code, of course):

```xml
<TextBox x:Name="_textBox">
    <TextBox.InputBindings>
        <local:MultiKeyBinding Command="{x:Static local:Commands.Secret}" Gesture="Ctrl+A,p,r,i,l,O,n,e,i,l,l,i,s,h,a,w,t"/>
    </TextBox.InputBindings>
</TextBox>
```

`MultiKeyGestureConverter` is used to parse the `Gesture` attribute in the XAML into a `MultiKeyGesture` for the `MultiKeyBinding`.

Note that I didn't put much effort into these supporting classes at all. They are bare-bones to get the demo working.

[Download the project]({{ page.assets }}MultiKeyGesture.zip).