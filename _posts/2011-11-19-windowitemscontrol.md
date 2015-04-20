---
title: WindowItemsControl
assets: /assets/2011-11-19-windowitemscontrol/
tags: [ ".NET", "WPF" ]
---
The application I'm currently working on - top secret, mum's the word, your death for my indiscretion, you get the idea - includes a widget-style interface. In order to render these widgets, I use an `ItemsControl` and bind it to a collection of view models, each of which represents a widget. I use a `Canvas` to lay them out according to their `XOffset` and `YOffset` properties. Something like this:

```XML
<ItemsControl ItemsSource="{Binding Widgets}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <Canvas/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemContainerStyle>
        <Style>
            <Setter Property="Canvas.Left" Value="{Binding XOffset}"/>
            <Setter Property="Canvas.Top" Value="{Binding YOffset}"/>
        </Style>
    </ItemsControl.ItemContainerStyle>
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <views:WidgetView/>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

This all works fine and I'd even go so far as to say it's a beautiful thing. However, it is only a widget-style interface by virtue of some trickery on my part. Without said trickery, it would be more of an MDI interface.

With an MDI interface, multiple child windows are contained within a parent window – they cannot appear outside the bounds of the parent. Whilst my widgets *look* like windows, they aren't. They're just regular WPF user controls with some additional smarts to allow positioning and what-have-you. And whilst it *looks* like they're free to roam wherever they like on the desktop, they're not. They're all contained within the `ItemsControl`, which is within the only `Window` in my application. Thus, they cannot be positioned outside the area in which the `Window` resides. It just so happens that I've stretched that `Window` across the entire desktop and made it transparent. This gives the illusion that I have a widget interface and am thus as cool as *The Dude* himself, but I actually have an MDI interface which makes me more *Napolean Dynamite* than *Lebowski*. Before the dance, that is.

All this wouldn't concern me terribly (one learns to live with it) but my users are actually privy to more screen real estate than I. And when I say "more", I mean they have six screens whilst I have two, one of which I can't use for anything of import because it frequently distorts and shows other signs of discontent (no audible screams as yet). Because of this abundance of screen real estate, my Window has to to stretch across a huge expanse of pixels in order to keep up this illusion of cool. This has dire consequences for performance. You see, if one's `Window` size exceeds the maximum texture size of one's video card (I don't have one of course, but my users do) then the video card won't be able to accelerate rendering of said `Window`. The `Window` will be software-rendered instead, which is probably going to be a lot slower and less capable of Dude-worthy effects and animations.

No problem, you say. Just host your widgets inside windows instead and be done with it. And this is indeed what I am going to do.

But I want it to be seamless with respect to the current code base. I don't want to have to go hook up a bunch of event handlers to create/show/close windows when my widget collection changes. I don't want to have to change the way my view models keep track of widget positions and sizes (all persisted across application restarts, of course). All I want to do is change this:

```XML
<ItemsControl ItemsSource="{Binding Widgets}">
```

to this:

```XML
<WindowItemsControl ItemsSource="{Binding Widgets}">
```

But unfortunately WPF doesn't have a `WindowItemsControl`. **Boo**. And it doesn't seem as though anyone in the community has written one.

Obviously, then, I set out to write my own.

My initial approach failed, but it's worth discussing anyway. I tried to have my `WindowItemsControl` create `Window` instances as containers. This failed because internal WPF code was attempting to add these `Window`s as visual children of the `ItemsControl`, and `Window`s must be top-level visual items (makes sense). So I tried to hack around this because I really wanted the logical connection between the `Window` and the `WindowItemsControl`, much the same way there's a logical connection between a `ListBoxItem` and its containing `ListBox`. If I could trick WPF into forgoing the visual connection, I could then attempt the logical connection.

Well, I tried all sorts of nastiness, and ended up reflectively invoking an internal member to trick WPF into not including the `Window` as a visual child. Success! Right!? Alas, no, because when I then added the `Window` as a logical child of the `WindowItemsControl`, I got another similar error. I can't remember the details, nor can I explain why a `Window` cannot be a logical child of another control (it's only a logical connection, after all). But it didn't work and I gave up on this approach entirely.

My second approach is much more sane but gives up on creating a logical connection between the `Window`s and their host. But I don't really need that anyway – it was a nice-to-have.

What I did instead was had the `WindowItemsControl` create `WindowItemsControlItem` instances as containers. These containers are really just surrogates for the `Window` they represent. When they're initialized, they display the `Window`. When they're destroyed, they close the `Window`. In addition, if a `Window` is closed ahead of time, the corresponding data item is removed from the underlying collection and thus too the surrogate from the visual tree.

The code is actually quite neat and compact. Here is the code for `WindowItemsControl`:

```C#
public class WindowItemsControl : ItemsControl
{
    public static readonly DependencyProperty ShowDialogProperty = DependencyProperty.Register(
        "ShowDialog",
        typeof(bool),
        typeof(WindowItemsControl));
 
    public static readonly DependencyProperty OwnerProperty = DependencyProperty.Register(
        "Owner",
        typeof(Window),
        typeof(WindowItemsControl),
        new FrameworkPropertyMetadata(OnOwnerChanged));
 
    public static readonly DependencyProperty WindowStartupLocationProperty = DependencyProperty.Register(
        "WindowStartupLocation",
        typeof(WindowStartupLocation),
        typeof(WindowItemsControl));
 
    public static readonly DependencyProperty RemoveDataItemWhenWindowClosedProperty = DependencyProperty.Register(
        "RemoveDataItemWhenWindowClosed",
        typeof(bool),
        typeof(WindowItemsControl),
        new FrameworkPropertyMetadata(true));
 
    static WindowItemsControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(WindowItemsControl), new FrameworkPropertyMetadata(typeof(WindowItemsControl)));
    }
 
    public bool ShowDialog
    {
        get { return (bool)this.GetValue(ShowDialogProperty); }
        set { this.SetValue(ShowDialogProperty, value); }
    }
 
    public Window Owner
    {
        get { return this.GetValue(OwnerProperty) as Window; }
        set { this.SetValue(OwnerProperty, value); }
    }
 
    public WindowStartupLocation WindowStartupLocation
    {
        get { return (WindowStartupLocation)this.GetValue(WindowStartupLocationProperty); }
        set { this.SetValue(WindowStartupLocationProperty, value); }
    }
 
    public bool RemoveDataItemWhenWindowClosed
    {
        get { return (bool)this.GetValue(RemoveDataItemWhenWindowClosedProperty); }
        set { this.SetValue(RemoveDataItemWhenWindowClosedProperty, value); }
    }
 
    protected override DependencyObject GetContainerForItemOverride()
    {
        return new WindowItemsControlItem(this);
    }
 
    protected override bool IsItemItsOwnContainerOverride(object item)
    {
        return item is WindowItemsControlItem;
    }
 
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        (element as WindowItemsControlItem).Window.Content = item;
    }
 
    protected override bool ShouldApplyItemContainerStyle(DependencyObject container, object item)
    {
        // the item container style will be applied to the windows, not to the containers (which are surrogates for the window)
        return false;
    }
 
    private static void OnOwnerChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        var windowItemsControl = (WindowItemsControl)dependencyObject;
        var owner = (Window)e.NewValue;
 
        for (var i = 0; i < windowItemsControl.Items.Count; ++i)
        {
            var container = windowItemsControl.ItemContainerGenerator.ContainerFromIndex(i) as WindowItemsControlItem;
 
            if (container == null)
            {
                continue;
            }
 
            container.Window.Owner = owner;
        }
    }
}
```

Pretty straightforward stuff. Note the following:

* it declares some properties (`ShowDialog`, `Owner`, `WindowStartupLocation`) that assist it in the display of child `Window`s
* it declares a `RemoveDataItemWhenWindowClosed` property that can be used to prevent the control from removing data items when a window is closed. This can be useful in shutdown or other situations where windows are being closed programmatically rather than by the user
* I don't apply the `ItemContainerStyle` to the containers themselves, but instead hold out so that I can apply them to the `Window`s they represent
* I also make sure that any change of `Owner` is applied to any existing `Window`s
* the default style is overridden to remove unnecessary stuff like the `Border`, because the `WindowItemsControl` will never actually be visible on screen

The `WindowItemsControl` works in conjunction with the `WindowItemsControlItem`, which looks like this:

```C#
public class WindowItemsControlItem : FrameworkElement
{
    private readonly WindowItemsControl windowItemsControl;
    private readonly Window window;
 
    static WindowItemsControlItem()
    {
        // there is no need for these items to be visible as they are simply surrogates for the windows that they display
        VisibilityProperty.OverrideMetadata(typeof(WindowItemsControlItem), new FrameworkPropertyMetadata(Visibility.Collapsed));
    }
 
    public WindowItemsControlItem(WindowItemsControl windowItemsControl)
    {
        windowItemsControl.AssertNotNull("windowItemsControl");
 
        this.windowItemsControl = windowItemsControl;
        this.window = this.CreateWindow(windowItemsControl);
 
        this.Loaded += delegate
        {
            if (this.windowItemsControl.ShowDialog)
            {
                this.window.ShowDialog();
            }
            else
            {
                this.window.Show();
            }
        };
 
        this.Unloaded += delegate
        {
            this.window.Close();
        };
    }
 
    public Window Window
    {
        get { return this.window; }
    }
 
    private Window CreateWindow(WindowItemsControl windowItemsControl)
    {
        var window = new Window
        {
            Owner = windowItemsControl.Owner,
            WindowStartupLocation = windowItemsControl.WindowStartupLocation
        };
 
        BindingOperations.SetBinding(window, Window.DataContextProperty, new Binding("Content") { Source = window });
        BindingOperations.SetBinding(window, Window.StyleProperty, new Binding("ItemContainerStyle") { Source = windowItemsControl });
        BindingOperations.SetBinding(window, Window.ContentTemplateProperty, new Binding("ItemTemplate") { Source = windowItemsControl });
        BindingOperations.SetBinding(window, Window.ContentTemplateSelectorProperty, new Binding("ItemTemplateSelector") { Source = windowItemsControl });
 
        window.Closed += delegate
        {
            // orphan the content because it might be hosted somewhere else later (including in another window)
            window.Content = null;
 
            // if the window closes, attempt to remove the original item from the underlying collection, which will result in this surrogate being removed too
            if (windowItemsControl.RemoveDataItemWhenWindowClosed)
            {
                var editableItems = windowItemsControl.Items as IEditableCollectionView;
 
                if (editableItems != null && editableItems.CanRemove)
                {
                    editableItems.Remove(this.DataContext);
                }
            }
        };
 
        return window;
    }
}
```

This is all pretty self-explanatory, too. The important points to note are:

* relevant properties on the `WindowItemsControl` are bound to the correct properties on the `Window`s themselves
* `Window`s are displayed when the surrogate is initialized, and closed when the surrogate is unloaded
* as mentioned earlier, `Window`s that are closed before the surrogate is destroyed (perhaps by the user clicking the close button) result in the related data item in the underlying collection being removed (unless the `RemoveDataItemWhenWindowClosed` property has been set to `false`). This, in turn, will cause the surrogate to be removed from the visual tree. In other words, if I close a widget `Window`, the corresponding `WidgetViewModel` will be removed from my collection of widget view models. Then, the `ItemsControl` will remove the related surrogate container from the visual tree.

Now, as I yearned for at the beginning of this post, I can simply change my `ItemsControl` to a `WindowItemsControl`, make minor adjustments to my `ItemContainerStyle` and it just magically works.

*The Dude abides.*

I created a little demo to show how it all works, which [you can download here]({{ page.assets }}WindowItemsControl.zip). Enjoy!