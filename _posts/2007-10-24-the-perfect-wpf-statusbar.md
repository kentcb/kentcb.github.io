---
title: The Perfect WPF StatusBar
assets: /assets/2007-10-24-the-perfect-wpf-statusbar/
tags: [ ".NET", "WPF" ]
---
Last night I was trying to incorporate a status bar into my WPF shell. It turns out getting the perfect behaviour wasn't as straightforward as I'd hoped, so I thought I'd elaborate here.

The simplest status bar is straightforward and looks just fine:

![Simple Status Bar]({{ page.assets }}simple_status_bar.png "Simple Status Bar")

The markup for this is:

```XML
<StatusBar>
    <StatusBarItem>
        <TextBlock>Ready</TextBlock>
    </StatusBarItem>
</StatusBar>
```

Now suppose you want to add a second panel (I wanted more, but I'll get to that). You might try:

```XML
<StatusBar>
    <StatusBarItem>
        <TextBlock>Ready</TextBlock>
    </StatusBarItem>
    <StatusBarItem>
        <TextBlock>Set</TextBlock>
    </StatusBarItem>
</StatusBar>
```

But this may not render how you were expecting:

![Dual Status Bar]({{ page.assets }}dual_status_bar.png "Dual Status Bar")

Typically you would want the left-most panel to stretch. Turns out the `StatusBarItem`s are hosted in a `DockPanel`, so you can do this just by setting the `Dock` property on the right panel:

```XML
<StatusBar>
    <StatusBarItem DockPanel.Dock="Right">
        <TextBlock>Set</TextBlock>
    </StatusBarItem>
    <StatusBarItem>
        <TextBlock>Ready</TextBlock>
    </StatusBarItem>
</StatusBar>
```

![Dual Docked Status Bar]({{ page.assets }}dual_docked_status_bar.png "Dual Docked Status Bar")

But notice something else: we had to switch the order of the `StatusBarItem`s so that the first one fills the remaining space. This is because of the way WPF's `DockPanel` works. The last item present in it will fill remaining space unless you tell it otherwise.

It's a bit confusing having the second item first, and things get worse as we add more and more items. Moreover, we don't have sufficient control over the proportional widths allocated to the status bar items.

We need more control over how the `StatusBarItem`s are arranged. The key to this problem is that the `StatusBar`'s use of a `DockPanel` for `StatusBarItem` layout is a default only. Like most things WPF, it can be customised. We can do this via the `StatusBar.ItemsPanel` property. This is actually a property of the `ItemsControl` class, which `StatusBar` inherits from. However, `StatusBar` does override the default panel type, changing it from `StackPanel` to `DockPanel`.

For the ultimate control, I opted to use a `Grid`, WPF's swiss army knife panel. For example:

```XML
<StatusBar>
    <StatusBar.ItemsPanel>
        <ItemsPanelTemplate>
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="4*"/>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
            </Grid>
        </ItemsPanelTemplate>
    </StatusBar.ItemsPanel>
    <StatusBarItem>
        <TextBlock>Ready</TextBlock>
    </StatusBarItem>
    <StatusBarItem Grid.Column="1">
        <ProgressBar Value="30" Width="80" Height="18"/>
    </StatusBarItem>
    <StatusBarItem Grid.Column="2">
        <TextBlock>Set</TextBlock>
    </StatusBarItem>
    <StatusBarItem Grid.Column="3">
        <TextBlock>Go!</TextBlock>
    </StatusBarItem>
</StatusBar>
```

With this markup we get:

![Perfect Status Bar]({{ page.assets }}perfect_status_bar.png "Perfect Status Bar")

Of course, being WPF you can add whatever you want to the status bar, including separators, images, or whatever. The point is, for more control over the layout of your `StatusBarItem`s, you'll want to swap out the default `DockPanel` for something with more *oomph*.