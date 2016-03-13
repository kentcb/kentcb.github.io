---
title: Beware DataContext and Equals Overrides
assets: /assets/2007-03-04-beware-datacontext-and-equals-overrides/
tags: [ ".NET", "WPF" ]
---
When binding to business or data objects in WPF, beware that WPF's `System.Windows.Data.BindingExpression` class uses `object.Equals()` to decide whether to refresh bindings or not. By default, this results in reference equality semantics. In other words, setting `DataContext` will result in bindings being refreshed so long as the new object is not exactly the same object instance that was already assigned to `DataContext`.

If your object (or one of its ancestors) overrides `Equals()` to use something other than reference equality semantics you need to be careful when re-assigning to `DataContext`. Suppose you have this business class:

```csharp
public sealed class Student : INotifyPropertyChanged
{
    private readonly int _id;
    private string _name;

    public int Id
    {
        get { ... }
    }

    public string Name
    {
        get { ... }
        set { ... }
    }

    public Student(int id, string name)
    {
        ...
    }

    public override bool Equals(object obj)
    {
        ...
        return _id == student._id;
    }
}
```

Note in particular that the `Student` class overrides `Equals()` such that two `Student` instances are considered equal if their IDs are equal. Now suppose you have a `StudentView` user control that binds to a `Student` instance:

```xml
<UserControl x:Class="BindingTest.StudentView"
    xmlns=http://schemas.microsoft.com/winfx/2006/xaml/presentation
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <StackPanel>
        <StackPanel Orientation="Horizontal">
            <Label>ID:</Label>
            <Label Content="{Binding Id}"/>
        </StackPanel> 
        <StackPanel Orientation="Horizontal">
            <Label>Name:</Label>
            <TextBox Text="{Binding Name}"/>
        </StackPanel>
    </StackPanel>
</UserControl>
```

Assuming that you re-use an instance of `StudentView` instead of recreating a new one every time, you may run into situations where assigning a new `Student` to `StudentView`'s `DataContext` does not update the bindings. This will happen where the new student's ID matches the ID of the student currently bound to. In this case, you will find that the name does not update in the view even if it has changed in the underlying `Student` instance. Actually, the ID wouldn't have updated either but it hasn't changed so you won't really notice.

The easiest way to fix this problem is to assign `null` to `DataContext` before assigning the new `Student`:

```csharp
studentView.DataContext = null;
studentView.DataContext = someStudent;
```

This will force the bindings to refresh regardless of whether the student IDs.

So when might all this craziness occur? For me, it happened due to the following:

1. our business layer objects all override `Equals` such that objects with the same type and ID are considered equal. This is necessary and correct behaviour for our application
2. our application is a smart client and has a background merge process that potentially replaces existing business objects in the client's data store with an updated instance of the business object (same ID but with differing details).
3. we're using CAB and hence tend to re-use views if they've already been created, rather than create a new instance of them. Of course, you don't need to be using CAB for this to be true - it just makes it a bit easier