---
title: Ugly Code for an Attractive UISearchBar
assets: /assets/2015-02-22-ugly-code-for-an-attractive-uisearchbar/
tags: [ "C#", ".NET", "Xamarin", "iOS" ]
---

Getting an iOS view to look *exactly* how you want it can be super frustrating. Take, for example, the simple requirement of hosting a `UISearchBar` inside a `UITableViewCell`. Here's some code as a starting point:

{% highlight C# %}
namespace UISearchBarExample
{
    using System;
    using Foundation;
    using UIKit;

    public class View : UITableViewController
    {
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            this.TableView.Source = new Source();
        }

        private class Source : UITableViewSource
        {
            public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
            {
                var cell = new UITableViewCell();
                UIView child = null;

                if (indexPath.Row == 0)
                {
                    child = new UILabel
                    {
                        Text = "Some existing thing"
                    };
                }
                else
                {
                    child = new UISearchBar
                    {
                        Placeholder = "Search for another thing"
                    };
                }

                cell.AddSubview(child);
                cell.ConstrainLayout(() =>
                    child.Left() == cell.Left() + Layout.StandardSuperviewSpacing &&
                    child.Right() == cell.Right() - Layout.StandardSuperviewSpacing &&
                    child.Top() == cell.Top() + Layout.StandardSiblingViewSpacing &&
                    child.Bottom() == cell.Bottom() - Layout.StandardSiblingViewSpacing);

                return cell;
            }

            public override nint RowsInSection(UITableView tableview, nint section)
            {
                return 2;
            }
        }
    }
}
{% endhighlight %} 

Obviously I'm not advocating this specific approach in a real app - I'm just getting to the crux of this post. Here is what this looks like when run (iPhone 4):

![First Attempt]({{ page.assets }}first-attempt.png "First Attempt")

Clearly, that's pretty ugly. We really need to lose the dark rectangle behind the search bar. You'd be forgiven for thinking it a simple matter of changing a property or two on `UISearchBar`. Perhaps `BackgroundColor`? Or maybe `SearchBarStyle`?

Turns out, changing the color properties (`BackgroundColor`, `BarTintColor`, `TintColor`) to `UIColor.Clear` actually makes matters worse because the background becomes solid black rather than gray. However, changing `SearchBarStyle` to `Minimal` gets us a small step forward:

![Second Attempt]({{ page.assets }}second-attempt.png "Second Attempt")

This is slightly more attractive, though still not ideal. I'd like for the search bar to be less conspicuous still. I want to lose the gray rectangle with rounded corners.

Despite all the advice I could find to the contrary, this is simply not possible in iOS 8 (probably not in 7 either). I had to resort to some ugly code to make this happen...by digging through the visual tree to find the `UITextField` inside the `UISearchBar`, we can then make the necessary adjustments.

I started with an extension method to make it easier to traverse the visual tree and find a particular view:

{% highlight C# %}
public static class VisualTreeExtensions
{
    public static T FindChildRecursively<T>(this UIView @this, Func<T, bool> predicate)
        where T : class
    {
        @this.AssertNotNull(nameof(@this));

        return FindChildRecursivelyImpl(@this, predicate);
    }

    private static T FindChildRecursivelyImpl<T>(UIView current, Func<T, bool> predicate)
        where T : class
    {
        T castCurrent = current as T;

        if (castCurrent != null && predicate(castCurrent))
        {
            return castCurrent;
        }

        foreach (var child in current.Subviews)
        {
            var findInChild = FindChildRecursivelyImpl(child, predicate);

            if (findInChild != null)
            {
                return findInChild;
            }
        }

        return default(T);
    }
}
{% endhighlight %}

With that, I could then find the `UITextField` and make the necessary adjustments:

{% highlight C# %}
child = new UISearchBar
{
    SearchBarStyle = UISearchBarStyle.Minimal,
    Placeholder = "Search for a new thing"
};
var textField = child.FindChildRecursively<UITextField>(_ => true);
textField.BorderStyle = UITextBorderStyle.None;
{% endhighlight %}

With that change, my UI now looks like I want it to:

![Third Attempt]({{ page.assets }}third-attempt.png "Third Attempt")