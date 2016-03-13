---
title: Dispatcher Frames
assets: /assets/2008-04-28-dispatcher-frames/
tags: [ ".NET", "WPF" ]
---
With scant exception, WPF controls must be accessed from the same thread on which they were created. Because of this, we say they have 'thread affinity'.

In WPF, thread affinity is realised via `DispatcherObject`s, which can be accessed directly only on the thread on which they were created. Alternatively, they can be accessed via their `Dispatcher`. The `Dispatcher` contains methods for adding operations to a queue which is processed on the `Dispatcher`'s thread. Each thread has at most one `Dispatcher` instance associated with it (which is created on demand). Many WPF classes - including controls such as `TextBox` and `Button` - inherit from `DispatcherObject`. This means that accessing instances of these controls must be done on the control's thread, possibly via the control's `Dispatcher`.

You may have noticed a type called `DispatcherFrame` whilst browsing MSDN (as you may be inclined to do on a Saturday night. Or not). That's what I want to discuss in this post.

You can think of a `DispatcherFrame` as something that forces operations to be processed until some condition is met. Every WPF application has at least one `DispatcherFrame` that is created when you run the application. It will continue pumping operations until the application is shut down.

At times it can be useful to pump operations until some condition of your own is met. Perhaps your code is executing on the UI thread and you want to empty the queue of operations in the `Dispatcher` to force control and screen updates. In the Winforms world, this is achieved via a call to `Application.DoEvents()`. In WPF, there is no equivalent method, but we can easily simulate one:

```csharp
public static void DoEvents(this Application application)
{
    DispatcherFrame frame = new DispatcherFrame();
    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new ExitFrameHandler(frm => frm.Continue = false), frame);
    Dispatcher.PushFrame(frame);
}
 
private delegate void ExitFrameHandler(DispatcherFrame frame);
```

The above code is part of a small demo project, which you can [download here]({{ page.assets }}DoEvents.zip). It adds a `DoEvents()` method to WPF's `Application` class. The `DoEvents()` method adds a `Background` priority operation to the `Dispatcher`'s queue, and then pushes a new frame that pumps operations until that operation is processed. As a result, all other operations that were already queued in the `Dispatcher` will be processed. There are several important things I need to point out here:

1. adding the new operation to the `Dispatcher` is done asynchronously. As such, the operation is not processed until we call `PushFrame()` 
2. the choice of `Background` as the priority is important. If we were to choose a higher priority (such as `Render`), then we would potentially leave lower-priority operations in the queue after the frame had exited. That's because our operation would be executed prior to those lower-priority operations, and thus we'd end the frame too soon. Perhaps there's value in adding a `DoEvents()` overload that accepts a minimum priority of operations that should be executed, but I haven't included one in the download
3. operations in a `Dispatcher` are not associated with a particular `DispatcherFrame`. A `DispatcherFrame` can cause any queued operation to be processed, not just those added after the frame is pushed
4. the code assumes it is running on the thread whose `Dispatcher` should be pumped

A `DoEvents()` implementation is nice for illustrating the utility of `DispatcherFrame`s (indeed it is used in the MSDN documentation too), but I believe it is unnecessary. As you can see in the demo, it is possible to simulate `DoEvents()` in a much simpler fashion: by synchronously adding a low-priority no-op to the `Dispatcher`'s queue:

```csharp
public static void DoEvents(this Application application)
{
    Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new VoidHandler(() => { }));
}

private delegate void VoidHandler();
```

The call to `Invoke()` will not return until our operation has been processed, which won't be until all higher priority operations are processed. Therefore, the end result is the same - all queued operations are pumped (apart from those with a priority lower than `Background`, but I'm ignoring that in this post).

So where would you use a `DispatcherFrame`?

I recently had need for one at my day job. We have a splash screen that usually just shows progress information (downloading files, searching for modules etcetera) but sometimes needs to ask the user which catalog they would like to load (a catalog is just a set of modules).

The splash presenter tries as hard as it can not to ask the user (if they only have one catalog, or it has been specified via a command line switch then it doesn't need to ask), but if it does need to, it delegates a call to the splash view on the UI thread. This call needs to return the catalog that the user selected. However, it has been called on the UI thread, so obtaining a catalog from the user requires pumping operations so they can select one from a list. The code looks like this:

```csharp
public Catalog SelectCatalog(ICollection<Catalog> availableCatalogs)
{
    StatusLabel = "Please select a catalog:";
    _catalogsItemsControl.ItemsSource = availableCatalogs;
 
    _catalogSelectionFrame = new DispatcherFrame(true);
    Dispatcher.PushFrame(_catalogSelectionFrame);
    StatusLabel = "Thank you";     _catalogsItemsControl.ItemsSource = null;
 
    return _selectedCatalog;
}
```

This method sets up the catalogs for the user to select from by assigning them to an `ItemsControl` (which renders each catalog as a hyperlink). It then pushes a new `DispatcherFrame`, which blocks until the frame is stopped. After the `DispatcherFrame` finishes, the selected catalog is returned. The only way to end the frame is by clicking on one of the hyperlinks representing the catalogs. Each hyperlink rendered by the `ItemsControl` has a Click handler as follows:

```csharp
private void _hyperlink_Click(object sender, EventArgs e)
{
    _selectedCatalog = (sender as Hyperlink).DataContext as Catalog;
    _catalogSelectionFrame.Continue = false;
}
```

When a hyperlink is clicked, I store the selected catalog and then end the `DispatcherFrame`. As a result, the code that pushed the `DispatcherFrame` will continue execution. Note that I would not have to manually track the selected catalog if I was using a `ListBox`, but for now I'm using with the `ItemsControl` because it was simpler to style.

Above I suggested that the only way to end the `DispatcherFrame` was by setting `DispatcherFrame.Continue` to `false`. That's not the entire story. `DispatcherFrame`s may also be asked to stop during application shutdown. When that happens, `DispatcherFrame`s will, by default, comply with the request. However, there is a constructor overload that allows you to bypass this behaviour and instead continue processing operations until your condition is met. This might be useful for a short-lived frame whose completion is critical to the correctness of your application.

[Download the Sample]({{ page.assets }}DoEvents.zip).