---
title: Virtual Paging in Silverlight
assets: /assets/2010-07-29-virtual-paging-in-silverlight/
tags: [ ".NET", "Silverlight" ]
---
Silverlight 3 added the [`IPagedCollectionView`](http://msdn.microsoft.com/en-us/library/system.componentmodel.ipagedcollectionview%28VS.95%29.aspx) interface (and concrete implementation [`PagedCollectionView`](http://msdn.microsoft.com/en-us/library/system.windows.data.pagedcollectionview%28VS.95%29.aspx)) to support paging through a large result set. The `DataPager` control can work with an implementation of `IPagedCollectionView` to allow users to page through the large data set by clicking on a page number. It typically looks like this:

![Data Pager]({{ page.assets }}data_pager.png "Data Pager")

Unfortunately (you knew it was coming), these new types don't offer support for virtual paging. Virtual paging is where you're paging data behind the scenes, but the user isn't explicitly changing pages as they do with the `DataPager` control. To them, the data just appears in a big ol' list. As they scroll around the list, data is paged in as necessary. If they move slowly down the list, they would quite possibly never know that data is being paged.

In the WPF world, it is possible to achieve virtual paging by implementing a collection view that simply retrieves data lazily if and when it is requested. This works because WPF's binding infrastructure will only request items in your source collection if it needs them. Alas, this is not the case in Silverlight. Silverlight will aggressively iterate over all items in your underlying collection regardless of whether they're visible. Thus, if you attempt the WPF trick of lazily loading data on request, you'll end up paging in all the data as soon as the binding kicks in. How annoying.

I tried a few things to make sure that Silverlight really was that dumb. For example, I tried providing an `IList<T>` implementation to see whether it would use the `Count` property to avoid the iteration over all the data, figuring perhaps it just needed to know how many items were in the data source in order to calculate the scrollable extent. No dice. I tried a few other things, but all attempts at thwarting Silverlight's aggressive iteration over the data source came up empty-handed.

But the thing is, I feel really strongly about the usability benefits of virtual paging versus manual paging. The former is far more intuitive and simple to use. I was simply not prepared to forgo this feature in my app, so I pressed on.

I came up with something of a compromise that goes like this: fine then, Miss Silverlight, if you insist then you may iterate over my entire source, but I'll just be giving you handles to the data. Those handles won't result in data being loaded - that will only happen when the handle's value is de-referenced. The value will only be de-referenced by bound UI components. And by virtue of UI virtualization, that will only happen when the user scrolls to within the region of that data.

So what do the handles look like? Initially I experimented with using `Lazy<T>` since it's built-in and seemed apropos. It actually worked fine when used synchronously. But loading the data on the UI thread would be really bad, effectively causing the UI to hang when the user scrolls through the data. When I attempted to extend `Lazy<T>` and add asynchronous support, things got ugly. Therefore, I scrapped it altogether and instead wrote my own `AsyncLazy<T>` class. On the surface, it looks a lot like `Lazy<T>`:

![Class Diagram]({{ page.assets }}class_diagram.png "Class Diagram")

But is has some key differences in both behavior and API. Firstly, accessing `Value` when the value is yet to be loaded will instantly return null. But it will also start resolving the value on a background thread, after which the `Value` property is updated with the result. The `IsValueCreated` property will not return true until the background thread has done its thing and Value is set. Crucially, `AsyncLazy<T>` implements `INotifyPropertyChanged` so that any UI elements bound to `Value` or `IsValueCreated` will refresh accordingly as values load.

I then have a collection view called `LazyAsyncCollectionView<T>`. This is an abstraction over the large underlying data set that will be consumed by your view components. You tell it the total number of items in the collection, the size of each page, and give it a lambda with which it can load a specific page. It then takes care of ensuring that pages are loaded as and when they are required. Importantly, it's actually an `IEnumerable<AsyncLazy<T>>` rather than `IEnumerable<T>`. This means that Silverlight can iterate over the entire view without us needing to load all the data. The data will only be loaded if the `Value` property is de-referenced, which is instigated by bindings in the view.

The view needs to be aware of the fact that it's bound to an `AsyncLazy<T>` rather than a `T`. Ergo, bindings look like this:

{% highlight XML %}
<TextBlock Text="{Binding Value.Property}"/>
{% endhighlight %}

where `Value` is the aforementioned property on `AsyncLazy<T>`. If you have a lot of properties and you don't like dereferencing `Value` all the time, you could bind a container's `DataContext` like this:

{% highlight XML %}
<StackPanel DataContext="{Binding Value}">
    <TextBlock Text="{Binding SomeProperty}"/>
    <TextBlock Text="{Binding SomeOtherProperty}"/>
</StackPanel>
{% endhighlight %}

The demo I put together uses all this stuff to display a list of people, the specifics of which you control (being a demo). As pages are being loaded, it displays a simple animation as a placeholder for the data:

![Sample Screenshot]({{ page.assets }}sample_screenshot.png "Sample Screenshot")

I mentioned that this whole thing was a compromise. Here are the primary weaknesses of the approach:

* the need for an intermediary type is regrettable, but – as explained above – necessary given the current limitations of Silverlight’s binding infrastructure
* it is not a true virtualization solution in the sense that it will not unload data. So if the user views the entire result set, all data will be loaded into memory. It is likely possible to alter the code so that it keeps a maximum number of pages in memory and intelligently unloads data where possible. I may well do that when I integrate this into my project.
* there is still the possibility that a very large number of `AsyncLazy<T>` instances will be created, thereby draining memory and CPU cycles. If there is potential for your underlying list to surpass a certain threshold (say, 100,000 entries), it would be prudent to set an upper bound on how many records you will display. After all, is it really that useful to allow users access to all that data? Personally, I will be setting an upper limit of 10,000 and including a message to the effect of "please refine your search" if the server reports more than that number of entries in the data set
* as discussed above, the UI needs to bind indirectly to the underlying data via the `Value` property on `AsyncLazy<T>`. Whilst not ideal, this is a price I am willing to pay in order to get this feature. It's possible you could implement `AsyncLazy<T>` as a `DynamicObject` that forwards property accesses onto the underlying data item. However, this could result in significant overhead and I haven't experimented with it yet

It's also incomplete. I'm literally blogging about this before I use it in anger, mostly because I'm just so excited I actually got it to work. However, my scenario may be simpler than yours. Here are some things that are missing/incomplete:

* error handling. What happens if the page fails to load? Currently, it just catches the exception and continuously retries until it succeeds. I will definitely be enhancing this, possibly to report errors via the `AsyncLazy<T>` class. That way, the UI could respond accordingly, perhaps by displaying a failure message and retry link.
* selection handling. It so happens that I do not need to support selection. I'm just displaying a large list of non-editable data (search results, if you're wondering). If you need selection support (e.g. if you use this with a `ListBox`), then you'll want to implement the pertinent members of `ICollectionView`, such as `CurrentItem` and `CurrentPosition`. I shouldn't imagine it would be too difficult
* grouping/sorting/filtering. If these were implemented, they would need to delegate back to the consumer of `LazyAsyncCollectionView<T>`. It doesn't make sense for these operations to occur on the client because all data would need to be loaded before it could be grouped, sorted, or filtered. However, if the server-side function supported these mechanisms, then it would make sense to delegate. I think I'll be adding sorting support for my own purposes, since my server-side function does support it. I need to think more about how I'm going to achieve this cleanly.
* the `AsyncLazy<T>` class assumes null is not a valid value for a data item. This is fine for my purposes, but may not be for you. If necessary, you could add a bool to track whether the value has been loaded rather than checking for null. Given the sheer number of `AsyncLazy<T>` instances that could be created, I was being careful to minimize the memory footprint of each instance. 1 `bool` is insignificant, but 1 million?

[Download the sample project]({{ page.assets }}VirtualPaging.zip).

Enjoy!