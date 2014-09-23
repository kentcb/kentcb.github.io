---
title: Using InheritanceBehavior to Aid Theme Development
assets: /assets/2011-08-29-using-inheritancebehavior-to-aid-theme-development/
tags: [ ".NET", "MEF", "Prism" ]
---
If you've ever put together a custom theme for WPF, you'll know the value of comparing your theme to the system theme. You can quickly spot things that you've missed, or compare and contrast behavior. However, if your theme targets controls by type rather than by key (and most themes should), how do you ensure your theme *isn't* applied to a given control? That is, how do you ensure a given control inherits the system theme regardless of the application-defined themes?

Suppose you're implementing a style for buttons. It looks like this:

{% highlight XML %}
<Style TargetType="Button">
    <Setter Property="Background">
        <Setter.Value>
            <LinearGradientBrush StartPoint="0,0" EndPoint="0,1">
                <GradientStop Offset="0" Color="#5f9ea0"/>
                <GradientStop Offset="0.5" Color="#3f7e80"/>
                <GradientStop Offset="0.5" Color="#7fbec0"/>
                <GradientStop Offset="1" Color="#1f5e80"/>
            </LinearGradientBrush>
        </Setter.Value>
    </Setter>
    <Setter Property="Foreground" Value="White"/>
    <Setter Property="FontFamily" Value="Bauhaus 93"/>
    <Setter Property="FontSize" Value="12pt"/>
    <Setter Property="FontWeight" Value="Normal"/>
</Style>
{% endhighlight %}

You want to put together a little test control that shows your themed `Button` against a system-themed `Button`:

{% highlight XML %}
<StackPanel> 
    <Button>System Theme</Button> 
    <Button>Application Theme</Button> 
</StackPanel>
{% endhighlight %}

But that won't work - both buttons will inherit your theme. In this case, you can simply tell the second `Button` not to apply your `Style`, thus ensuring it inherits the system-defined one:

{% highlight XML %}
<Button Style="{x:Null}">System Theme</Button>
{% endhighlight %}

But what if you're theming something far more complex, like the `DataGrid` control? It has child controls (`DataGridCell`, `DataGridRow`, `DataGridColumnHeader` etc), each with their own style. How can you stop all those child controls from inheriting the styles provided by your theme? Well, sometimes the parent control (`DataGrid` in this case) will expose properties that allow you to override the styles for child controls:

{% highlight XML %}
<DataGrid Style="{x:Null}" CellStyle="{x:Null}" RowStyle="{x:Null}" .../>
{% endhighlight %}

But this is tedious, error-prone, and not all controls will expose such properties. What, then, is the diligent WPF journeyman to do?

The best way around this problem I've found is to make use of the little-known [`FrameworkElement.InheritanceBehavior`](http://msdn.microsoft.com/en-us/library/system.windows.frameworkelement.inheritancebehavior.aspx) property. This `protected` property specifies how resource and property inheritance lookups should behave from any `FrameworkElement`. We can define a simple control with an inheritance behavior to meet our needs as follows:

{% highlight C# %}
public sealed class UseSystemTheme : ContentControl 
{ 
    public UseSystemTheme() 
    { 
        this.InheritanceBehavior = System.Windows.InheritanceBehavior.SkipToThemeNow; 
    } 
}
{% endhighlight %}

Then, to prevent a control from inheriting our themed `Style`, we can simply wrap it in a `UseSystemTheme`. Going back to our `Button` example, it would look like:

{% highlight XML %}
<StackPanel> 
    <local:UseSystemTheme> 
        <Button>System Theme</Button> 
    </local:UseSystemTheme> 
    <Button>Application Theme</Button> 
</StackPanel>
{% endhighlight %}

Now it's easy to ensure we're comparing the application theme against the system theme. Moreover, you can use the same approach regardless of how complex the contained elements are. So it’ll work whether you’re styling a `Button`, a `DataGrid`, or whatever.

![Screenshot]({{ page.assets }}screenshot.png "Screenshot")

NOTE: I actually made my `UseSystemTheme` control a little more complex than presented here. I had it override its default `Background` value and set it to `SystemColors.WindowBrush`. Then I had to provide a template for it because, by default, a `ContentControl` does not honor its `Background` property. These changes ensure that every `UseSystemTheme` instance has the default brush as its background instead of inheriting whatever else you've set in your theme. It’s the *child* of the `UseSystemTheme` instance whose resource lookup is affected by `InheritanceBehavior`, not the `UseSystemTheme` instance itself!