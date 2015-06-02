---
title: Reducing Auto Layout Friction
assets: /assets/2015-06-02-reducing-auto-layout-friction/
tags: [ "iOS", "auto-layout", "Xamarin", "C#" ]
---
In the 18 months or so I've been doing iOS development, my biggest struggles have been with layout. Over the years, iOS layout infrastructure has evolved drastically: from manually positioning views, to using springs, struts and auto-resizing masks. And with iOS 6 onwards, auto layout.

From early on I decided to utilise [auto layout](https://developer.apple.com/library/ios/documentation/UserExperience/Conceptual/AutolayoutPG/Introduction/Introduction.html) because it ostensibly offers more power whilst reducing the amount of work required to support multiple screen sizes and orientations (with some [cost to performance](http://floriankugler.com/2013/04/22/auto-layout-performance-on-ios/), of course). And it would save me having to learn about all the "legacy" layout stuff.

But even auto layout is far from a walk in the park. Here's an example of some C# code that creates an `NSLayoutConstraint` using the UIKit APIs:

```C#
var constraint = NSLayoutConstraint.Create(
    this.checkmarkImage,
    NSLayoutAttribute.Left,
    NSLayoutRelation.Equal,
    this.ContentView,
    NSLayoutAttribute.Left,
    1,
    20);
```

In this case, we're ensuring the left side of our `checkmarkImage` is positioned 20px in from the left side of `ContentView`. Just to be clear: that's *one* constraint. A non-trivial view is likely to have a dozen or more. I'm sure you can imagine the write-only code that would result.

So having decided to use auto layout I realised I needed to find a better way to leverage it into my code base. It wasn't long before I found Frank Krueger's ([@praeclarum](https://twitter.com/praeclarum)) [Easy Layout gist](https://gist.github.com/praeclarum/6225853). The idea of Frank's code was to allow constraints to be specified via expressions. Our example above could instead be written as:

```C#
this.ContentView.ConstrainLayout(() =>
    this.checkmarkImage.Left() == this.ContentView.Left() + 20);
```

This was an excellent starting point for facilitating auto layout within my iOS applications without requiring a wall of code. But over the months I've tweaked Frank's code to better suit my needs. I want to document and discuss most of those changes here.

*I've just noticed Frank has updated his gist several times too, and some of the things he's added mirror my changes. I'll point out where that's the case below.*

## Constants

OK, a small thing first. I wanted to remove the need for hard-coded sizes in constraint expressions. Most of the time we care about "standard" spacing, such as that between a view and its superview, and between sibling views. To that end, I added several constants to the `Layout` class:

```C#
public const int StandardSiblingViewSpacing = 8;
public const int HalfSiblingViewSpacing = StandardSiblingViewSpacing / 2;
public const int StandardSuperviewSpacing = 20;
public const int HalfSuperviewSpacing = StandardSuperviewSpacing / 2;

public const float RequiredPriority = (float)UILayoutPriority.Required;
public const float HighPriority = (float)UILayoutPriority.DefaultHigh;
public const float LowPriority = (float)UILayoutPriority.DefaultLow;
```

The first set of constants allows us to change our constraint to:   

```C#
this.ContentView.ConstrainLayout(() =>
    this.checkmarkImage.Left() == this.ContentView.Left() + Layout.StandardSuperviewSpacing);
```

Or, by using C# 6's `using static` feature:

```C#
this.ContentView.ConstrainLayout(() =>
    this.checkmarkImage.Left() == this.ContentView.Left() + StandardSuperviewSpacing);
```

The second group of constants allows us to more easily specify priorities when calling `SetContentHuggingPriority` and `SetContentCompressionResistancePriority`. Using `UILayoutPriority` directly means we need to cast:

```C#
this.checkmarkImage.SetContentHuggingPriority(
    (float)UILayoutPriority.DefaultHigh,
    UILayoutConstraintAxis.Horizontal);
```

Versus this:

```C#
this.checkmarkImage.SetContentHuggingPriority(
    Layout.HighPriority,
    UILayoutConstraintAxis.Horizontal);
```

## Improved Evaluation Support

One thing I really wanted was to be able to support constraint code like this:

```C#
var gapSize = someBoolean ? 0 : Layout.StandardSiblingViewSpacing;

cell.ConstrainLayout(() =>
    someView.Left() == label.Right() + gapSize);
```

Here the gap between `label` and `someView` is dynamically determined based on the value of `someBoolean`. This enabled certain scenarios that would otherwise have been far more painful. 

## Baseline Support

*Frank has since added support for this.*

Originally there was no way to constrain against a view's baseline. My code has a `LayoutExtensions` class that defines extension methods for all the properties you can constrain against:

* Width
* Height
* Top
* Bottom
* Left
* Right
* X (same as Left)
* Y (same as Top)
* CenterX
* CenterY
* Baseline
* Leading
* Trailing

Consequently, we can write constraints such as:

```C#
this.View.ConstrainLayout(() =>
    this.nameLabel.Baseline() == nameTextView.Baseline());
```

## Avoiding Compiler Warnings

In Xamarin Studio, comparing two `float` values results in compiler warnings. Because I use Xamarin Studio for some projects, this was incredibly annoying. Basically, I would see warnings like this everywhere:

![Compiler Warnings]({{ page.assets }}compiler-warnings.png "Compiler Warnings")

To solve this, I created the `LayoutExtensions` class mentioned above. By using extension methods rather than the existing properties (such as `Frame.Left`) I could both reduce the verbosity of constraint code, and get around the compiler warnings. The extension methods all return `int`:

```C#
public static int Left(this UIView @this) => 0;
```

Ultimately, the type doesn't matter because the method invocation is just a marker picked up by the expression parsing logic inside the `Layout` class. By returning `int` we're ensuring that all constraints are comparing one `int` to another, thus avoiding the compiler warnings.

## Priorities

*Frank has since added support for this.*

Sometimes we want our constraints to act at a lower priority, perhaps to avoid ambiguity between our constraints and system-provided constraints. To facilitate this, I added an optional `priority` parameter (of type `float`) to `ConstrainLayout`, which defaults to `Layout.RequiredPriority`.

## Naming Controls for Improved Diagnostics

From day 1, one thing I absolutely hated about auto layout were the error messages one sees when constraints cannot be satisfied. Here's an example:

```
Unable to simultaneously satisfy constraints.
	Probably at least one of the constraints in the following list is one you don't want. Try this: (1) look at each constraint and try to figure out which you don't expect; (2) find the code that added the unwanted constraint or constraints and fix it. (Note: If you're seeing NSAutoresizingMaskLayoutConstraints that you don't understand, refer to the documentation for the UIView property translatesAutoresizingMaskIntoConstraints) 
(
    "<NSLayoutConstraint:0x7fd04600 V:|-(8)-[UILabel:0x7fd22640' ']   (Names:'|':UITableViewCellContentView:0x7fd304d0 )>",
    "<NSLayoutConstraint:0x7fd03f00 V:|-(0)-[UILabel:0x7fd22640' ']   (Names:'|':UITableViewCellContentView:0x7fd304d0 )>"
)

Will attempt to recover by breaking constraint 
<NSLayoutConstraint:0x7fd04600 V:|-(8)-[UILabel:0x7fd22640' ']   (Names:'|':UITableViewCellContentView:0x7fd304d0 )>
```

Riiiiight...

I can't tell you how many times I wanted to throw my Mac in the pool as a result of these messages. Yes, we can spit out the `Handle` property of our various `UIView` somewhere and then manually match up controls those identifiers with those in the error message, but by all the gods that is painful.

For a long while I just put up with it - I didn't feel as though I had any recourse, considering [there is no way to set view identifiers in iOS](http://stackoverflow.com/questions/12965014/how-to-set-the-identifier-of-a-uiview-in-xcode-4-5-ios-for-debugging-auto-layout). But recently I was pushed over the edge by a constraint ambiguity message that I just could not fathom. To that end, I set out to solve the problem of the opaque error messages.  

It turned out to be really tricky. If it wasn't for the help of Xamarin's Rolf Kvinge ([@rolfkvinge](https://twitter.com/rolfkvinge)), I don't think I ever would have cracked this nut. The full details of my failed attempts are documented [in bugzilla](https://bugzilla.xamarin.com/show_bug.cgi?id=30387), so I won't bore you with the details here.

The eventual solution (again, thanks to Rolf) involves "swizzling", which is the dubious practice of replacing an existing selector at runtime. Because of this, and because this feature is solely for diagnostic purposes, almost all code related to naming controls is only included in `DEBUG` builds.

The upshot is that we can specify names for our controls like this (this will still build for non-`DEBUG` builds, but the calls to `Name` will have no effect):

```C#
this.ContentView.ConstrainLayout(() =>
    this.clientNameLabel.Top() == this.ContentView.Top() + Layout.StandardSiblingViewSpacing &&
    this.clientNameLabel.Top() == this.ContentView.Top() &&
    this.clientNameLabel.Name() == nameof(this.clientNameLabel) &&
    this.ContentView.Name() == nameof(this.ContentView));
```

And now our ambiguity results in this error message:

```
Unable to simultaneously satisfy constraints.
	Probably at least one of the constraints in the following list is one you don't want. Try this: (1) look at each constraint and try to figure out which you don't expect; (2) find the code that added the unwanted constraint or constraints and fix it. (Note: If you're seeing NSAutoresizingMaskLayoutConstraints that you don't understand, refer to the documentation for the UIView property translatesAutoresizingMaskIntoConstraints) 
(
    "<NSLayoutConstraint:0x7fed5410 V:|-(8)-[clientNameLabel' ']   (Names: '|':ContentView )>",
    "<NSLayoutConstraint:0x7fed5480 V:|-(0)-[clientNameLabel' ']   (Names: '|':ContentView )>"
)

Will attempt to recover by breaking constraint 
<NSLayoutConstraint:0x7fed5410 V:|-(8)-[clientNameLabel' ']   (Names: '|':ContentView )>
```

Much better!

## The Code

Firstly, if you want to enable the support for naming controls, be sure to include this first thing in your `AppDelegate`:

```C#
#if DEBUG
    Layout.DebugConstraint.Swizzle();
#endif
```

And here is all the code, including unit tests:

### Layout.cs

```C#
// this code is a heavily modified (and tested) version of https://gist.github.com/praeclarum/6225853

namespace iOS.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Foundation;
    using ObjCRuntime;
    using UIKit;

    public static class Layout
    {
        // the standard spacing between sibling views
        public const int StandardSiblingViewSpacing = 8;

        // half the standard spacing between sibling views
        public const int HalfSiblingViewSpacing = StandardSiblingViewSpacing / 2;

        // the standard spacing between a view and its superview
        public const int StandardSuperviewSpacing = 20;

        // half the standard spacing between superviews
        public const int HalfSuperviewSpacing = StandardSuperviewSpacing / 2;

        public const float RequiredPriority = (float)UILayoutPriority.Required;

        public const float HighPriority = (float)UILayoutPriority.DefaultHigh;

        public const float LowPriority = (float)UILayoutPriority.DefaultLow;

#if DEBUG

        internal static readonly IDictionary<string, string> constraintSubstitutions = new Dictionary<string, string>();

#endif

        public static void ConstrainLayout(this UIView view, Expression<Func<bool>> constraintsExpression, float priority = RequiredPriority)
        {
            var body = constraintsExpression.Body;
            var constraints = FindBinaryExpressionsRecursive(body)
                .Select(e =>
                {
#if DEBUG

                    if (ExtractAndRegisterName(e, view))
                    {
                        return null;
                    }

#endif

                    return CompileConstraint(e, view, priority);
                })
                .Where(x => x != null)
                .ToArray();

            view.AddConstraints(constraints);
        }

        private static IEnumerable<BinaryExpression> FindBinaryExpressionsRecursive(Expression expression)
        {
            var binaryExpression = expression as BinaryExpression;

            if (binaryExpression == null)
            {
                yield break;
            }

            if (binaryExpression.NodeType == ExpressionType.AndAlso)
            {
                foreach (var childBinaryExpression in FindBinaryExpressionsRecursive(binaryExpression.Left))
                {
                    yield return childBinaryExpression;
                }

                foreach (var childBinaryExpression in FindBinaryExpressionsRecursive(binaryExpression.Right))
                {
                    yield return childBinaryExpression;
                }
            }
            else
            {
                yield return binaryExpression;
            }
        }

#if DEBUG

        // special case to extract names from the expression, such as this.someControl.Name() == nameof(someControl)
        private static bool ExtractAndRegisterName(BinaryExpression binaryExpression, UIView constrainedView)
        {
            if (binaryExpression.NodeType != ExpressionType.Equal)
            {
                return false;
            }

            MethodCallExpression methodCallExpression;
            UIView view;
            NSLayoutAttribute layoutAttribute;
            DetermineConstraintInformationFromExpression(binaryExpression.Left, out methodCallExpression, out view, out layoutAttribute, false);

            if (methodCallExpression == null || methodCallExpression.Method.Name != nameof(LayoutExtensions.Name))
            {
                return false;
            }

            if (binaryExpression.Right.NodeType != ExpressionType.Constant)
            {
                throw new NotSupportedException("When assigning a name to a control, only constants are supported.");
            }

            var name = (string)((ConstantExpression)binaryExpression.Right).Value;
            var iOSName = view.Class.Name + ":0x" + view.Handle.ToString("x");
            constraintSubstitutions[iOSName] = name;

            return true;
        }

#endif

        private static NSLayoutConstraint CompileConstraint(BinaryExpression binaryExpression, UIView constrainedView, float priority)
        {
            NSLayoutRelation layoutRelation;

            switch (binaryExpression.NodeType)
            {
                case ExpressionType.Equal:
                    layoutRelation = NSLayoutRelation.Equal;
                    break;
                case ExpressionType.LessThanOrEqual:
                    layoutRelation = NSLayoutRelation.LessThanOrEqual;
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    layoutRelation = NSLayoutRelation.GreaterThanOrEqual;
                    break;
                default:
                    throw new NotSupportedException("Not a valid relationship for a constraint: " + binaryExpression.NodeType);
            }

            MethodCallExpression methodCallExpression;
            UIView leftView;
            NSLayoutAttribute leftLayoutAttribute;
            DetermineConstraintInformationFromExpression(binaryExpression.Left, out methodCallExpression, out leftView, out leftLayoutAttribute);

            if (leftView != null && leftView != constrainedView)
            {
                leftView.TranslatesAutoresizingMaskIntoConstraints = false;
            }

            UIView rightView;
            NSLayoutAttribute rightLayoutAttribute;
            float multiplier;
            float constant;
            DetermineConstraintInformationFromExpression(binaryExpression.Right, out rightView, out rightLayoutAttribute, out multiplier, out constant);

            if (rightView != null && rightView != constrainedView)
            {
                rightView.TranslatesAutoresizingMaskIntoConstraints = false;
            }

            var constraint = NSLayoutConstraint.Create(
                leftView,
                leftLayoutAttribute,
                layoutRelation,
                rightView,
                rightLayoutAttribute,
                multiplier,
                constant);
            constraint.Priority = priority;
            return constraint;
        }

        private static void DetermineConstraintInformationFromExpression(
            Expression expression,
            out MethodCallExpression methodCallExpression,
            out UIView view,
            out NSLayoutAttribute layoutAttribute,
            bool throwOnError = true)
        {
            methodCallExpression = FindExpressionOfType<MethodCallExpression>(expression);

            if (methodCallExpression == null)
            {
                if (throwOnError)
                {
                    throw new NotSupportedException("Constraint expression must be a method call.");
                }
                else
                {
                    view = null;
                    layoutAttribute = default(NSLayoutAttribute);
                    return;
                }
            }

            layoutAttribute = NSLayoutAttribute.NoAttribute;

            switch (methodCallExpression.Method.Name)
            {
                case nameof(LayoutExtensions.Width):
                    layoutAttribute = NSLayoutAttribute.Width;
                    break;
                case nameof(LayoutExtensions.Height):
                    layoutAttribute = NSLayoutAttribute.Height;
                    break;
                case nameof(LayoutExtensions.Left):
                case nameof(LayoutExtensions.X):
                    layoutAttribute = NSLayoutAttribute.Left;
                    break;
                case nameof(LayoutExtensions.Top):
                case nameof(LayoutExtensions.Y):
                    layoutAttribute = NSLayoutAttribute.Top;
                    break;
                case nameof(LayoutExtensions.Right):
                    layoutAttribute = NSLayoutAttribute.Right;
                    break;
                case nameof(LayoutExtensions.Bottom):
                    layoutAttribute = NSLayoutAttribute.Bottom;
                    break;
                case nameof(LayoutExtensions.CenterX):
                    layoutAttribute = NSLayoutAttribute.CenterX;
                    break;
                case nameof(LayoutExtensions.CenterY):
                    layoutAttribute = NSLayoutAttribute.CenterY;
                    break;
                case nameof(LayoutExtensions.Baseline):
                    layoutAttribute = NSLayoutAttribute.Baseline;
                    break;
                case nameof(LayoutExtensions.Leading):
                    layoutAttribute = NSLayoutAttribute.Leading;
                    break;
                case nameof(LayoutExtensions.Trailing):
                    layoutAttribute = NSLayoutAttribute.Trailing;
                    break;
                default:
                    if (throwOnError)
                    {
                        throw new NotSupportedException("Method call '" + methodCallExpression.Method.Name + "' is not recognized as a valid constraint.");
                    }
                    break;
            }

            if (methodCallExpression.Arguments.Count != 1)
            {
                if (throwOnError)
                {
                    throw new NotSupportedException("Method call '" + methodCallExpression.Method.Name + "' has " + methodCallExpression.Arguments.Count + " arguments, where only 1 is allowed.");
                }
                else
                {
                    view = null;
                    return;
                }
            }

            var viewExpression = methodCallExpression.Arguments.FirstOrDefault() as MemberExpression;

            if (viewExpression == null)
            {
                if (throwOnError)
                {
                    throw new NotSupportedException("The argument to method call '" + methodCallExpression.Method.Name + "' must be a member expression that resolves to the view being constrained.");
                }
                else
                {
                    view = null;
                    return;
                }
            }

            view = Evaluate<UIView>(viewExpression);

            if (view == null)
            {
                if (throwOnError)
                {
                    throw new NotSupportedException("The argument to method call '" + methodCallExpression.Method.Name + "' resolved to null, so the view to be constrained could not be determined.");
                }
                else
                {
                    view = null;
                    return;
                }
            }
        }

        private static void DetermineConstraintInformationFromExpression(
            Expression expression,
            out UIView view,
            out NSLayoutAttribute layoutAttribute,
            out float multiplier,
            out float constant)
        {
            var viewExpression = expression;

            view = null;
            layoutAttribute = NSLayoutAttribute.NoAttribute;
            multiplier = 1.0f;
            constant = 0.0f;

            if (viewExpression.NodeType == ExpressionType.Add || viewExpression.NodeType == ExpressionType.Subtract)
            {
                var binaryExpression = (BinaryExpression)viewExpression;
                constant = Evaluate<float>(binaryExpression.Right);

                if (viewExpression.NodeType == ExpressionType.Subtract)
                {
                    constant = -constant;
                }

                viewExpression = binaryExpression.Left;
            }

            if (viewExpression.NodeType == ExpressionType.Multiply || viewExpression.NodeType == ExpressionType.Divide)
            {
                var binaryExpression = (BinaryExpression)viewExpression;
                multiplier = Evaluate<float>(binaryExpression.Right);

                if (viewExpression.NodeType == ExpressionType.Divide)
                {
                    multiplier = 1 / multiplier;
                }

                viewExpression = binaryExpression.Left;
            }

            if (viewExpression is MethodCallExpression)
            {
                MethodCallExpression methodCallExpression;
                DetermineConstraintInformationFromExpression(viewExpression, out methodCallExpression, out view, out layoutAttribute);
            }
            else
            {
                // constraint must be something like: view.Width() == 50
                constant = Evaluate<float>(viewExpression);
            }
        }

        private static T Evaluate<T>(Expression expression)
        {
            var result = Evaluate(expression);

            if (result is T)
            {
                return (T)result;
            }

            return (T)Convert.ChangeType(Evaluate(expression), typeof(T));
        }

        private static object Evaluate(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Constant)
            {
                return ((ConstantExpression)expression).Value;
            }

            if (expression.NodeType == ExpressionType.MemberAccess)
            {
                var memberExpression = (MemberExpression)expression;
                var member = memberExpression.Member;

                if (member.MemberType == MemberTypes.Field)
                {
                    var fieldInfo = (FieldInfo)member;

                    if (fieldInfo.IsStatic)
                    {
                        return fieldInfo.GetValue(null);
                    }
                }
            }

            return Expression.Lambda(expression).Compile().DynamicInvoke();
        }

        // searches for an expression of type T within expression, skipping through "irrelevant" nodes
        private static T FindExpressionOfType<T>(Expression expression)
            where T : Expression
        {
            while (!(expression is T))
            {
                switch (expression.NodeType)
                {
                    case ExpressionType.Convert:
                        expression = ((UnaryExpression)expression).Operand;
                        break;
                    default:
                        return default(T);
                }
            }

            return (T)expression;
        }

#if DEBUG

        public static class DebugConstraint
        {
            private delegate IntPtr DescriptionDelegate(IntPtr self, IntPtr sel);
            private static DescriptionDelegate replacementDescriptionImplementation = new DescriptionDelegate(Description);

            public static void Swizzle()
            {
                var constraintClass = Class.GetHandle(typeof(NSLayoutConstraint));
                var method = class_getInstanceMethod(constraintClass, Selector.GetHandle("description"));
                var originalImpl = class_getMethodImplementation(constraintClass, Selector.GetHandle("description"));

                // add the original implementation to respond to 'customDescription'
                class_addMethod(constraintClass, Selector.GetHandle("customDescription"), originalImpl, "@@:");

                // replace the original implementation with our own for the 'descriptor' method.
                var newImpl = System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(replacementDescriptionImplementation);
                method_setImplementation(method, newImpl);
            }

            [ObjCRuntime.MonoPInvokeCallback(typeof(DescriptionDelegate))]
            public static IntPtr Description(IntPtr self, IntPtr sel)
            {
                var originalDescriptionPtr = objc_msgSend(self, Selector.GetHandle("customDescription"));
                var originalDescription = Runtime.GetNSObject<NSString>(originalDescriptionPtr);
                var description = originalDescription.ToString();

                foreach (var substitution in Layout.constraintSubstitutions)
                {
                    description = description.Replace(substitution.Key, substitution.Value);
                }

                return new NSString(description).Handle;
            }

            [System.Runtime.InteropServices.DllImport("libobjc.dylib")]
            static extern IntPtr objc_msgSend(IntPtr handle, IntPtr sel);

            [System.Runtime.InteropServices.DllImport("libobjc.dylib")]
            static extern IntPtr class_getInstanceMethod(IntPtr c, IntPtr sel);

            [System.Runtime.InteropServices.DllImport("libobjc.dylib")]
            static extern bool class_addMethod(IntPtr cls, IntPtr name, IntPtr imp, string types);

            [System.Runtime.InteropServices.DllImport("libobjc.dylib")]
            extern static IntPtr class_getMethodImplementation(IntPtr cls, IntPtr sel);

            [System.Runtime.InteropServices.DllImport("libobjc.dylib")]
            extern static IntPtr method_setImplementation(IntPtr method, IntPtr imp);
        }

#endif
    }
}
```

### LayoutExtensions.cs

```C#
namespace iOS.Utility
{
    using UIKit;

    // provides extensions that should be used when laying out via the Layout class
    // note the use of ints here rather than floats because comparing floats in our constraint expressions results in annoying compiler warnings
    public static class LayoutExtensions
    {
        public static int Width(this UIView @this) => 0;

        public static int Height(this UIView @this) => 0;

        public static int Left(this UIView @this) => 0;

        public static int X(this UIView @this) => 0;

        public static int Top(this UIView @this) => 0;

        public static int Y(this UIView @this) => 0;

        public static int Right(this UIView @this) => 0;

        public static int Bottom(this UIView @this) => 0;

        public static int Baseline(this UIView @this) => 0;

        public static int Leading(this UIView @this) => 0;

        public static int Trailing(this UIView @this) => 0;

        public static int CenterX(this UIView @this) => 0;

        public static int CenterY(this UIView @this) => 0;

        public static string Name(this UIView @this) => null;
    }
}
```

### LayoutFixture.cs

```C#
namespace UnitTests.iOS.Utility
{
    using System;
    using UIKit;
    using iOS.Utility;
    using Xunit;

    public sealed class LayoutFixture
    {
        [Fact]
        public void constrain_layout_throws_if_relationship_type_is_invalid()
        {
            var view = new UIView();
            var ex = Assert.Throws<NotSupportedException>(() => view.ConstrainLayout(() => view.Left() > view.Right()));
            Assert.Equal("Not a valid relationship for a constraint: GreaterThan", ex.Message);
        }

        [Fact]
        public void constrain_layout_throws_if_constraint_is_not_a_method_call()
        {
            var view = new UIView();
            var ex = Assert.Throws<NotSupportedException>(() => view.ConstrainLayout(() => view.ExclusiveTouch == true));
            Assert.Equal("Constraint expression must be a method call.", ex.Message);
        }

        [Fact]
        public void constrain_layout_throws_if_method_is_not_recognized()
        {
            var view = new UIView();
            var ex = Assert.Throws<NotSupportedException>(() => view.ConstrainLayout(() => view.GetType() == null));
            Assert.Equal("Method call 'GetType' is not recognized as a valid constraint.", ex.Message);
        }

        [Fact]
        public void constrain_layout_throws_if_the_method_call_has_the_wrong_number_of_arguments()
        {
            var view = new UIView();
            var viewImposter = new ViewImposter();
            var ex = Assert.Throws<NotSupportedException>(() => view.ConstrainLayout(() => viewImposter.Right(0, 0) == viewImposter.Right(0, 0)));
            Assert.Equal("Method call 'Right' has 2 arguments, where only 1 is allowed.", ex.Message);
        }

        [Fact]
        public void constrain_layout_throws_if_the_argument_to_the_method_is_not_a_member_expression()
        {
            var view = new UIView();
            var viewImposter = new ViewImposter();
            var ex = Assert.Throws<NotSupportedException>(() => view.ConstrainLayout(() => viewImposter.Left(0) == viewImposter.Left(0)));
            Assert.Equal("The argument to method call 'Left' must be a member expression that resolves to the view being constrained.", ex.Message);
        }

        [Fact]
        public void constrain_layout_throws_if_view_is_null()
        {
            UIView view = null;
            var ex = Assert.Throws<NotSupportedException>(() => view.ConstrainLayout(() => view.Left() == view.Right()));
            Assert.Equal("The argument to method call 'Left' resolved to null, so the view to be constrained could not be determined.", ex.Message);
        }

        [Fact]
        public void constrain_layout_allows_constraints_with_no_multiplier_or_constant_to_be_configured()
        {
            var superView = new UIView();
            var view = new UIView();

            superView.InvokeOnMainThread(
                () =>
                {
                    superView.AddSubview(view);
                    superView.ConstrainLayout(() => view.Left() == superView.Left());

                    var constraints = superView.Constraints;
                    Assert.Equal(1, constraints.Length);

                    Assert.Same(view, constraints[0].FirstItem);
                    Assert.Equal(NSLayoutAttribute.Left, constraints[0].FirstAttribute);
                    Assert.Same(superView, constraints[0].SecondItem);
                    Assert.Equal(NSLayoutAttribute.Left, constraints[0].SecondAttribute);
                    Assert.Equal(1f, constraints[0].Multiplier);
                    Assert.Equal(0f, constraints[0].Constant);
                });
        }

        [Fact]
        public void constrain_layout_allows_constraints_with_a_multiplier_to_be_configured()
        {
            var superView = new UIView();
            var view = new UIView();

            superView.InvokeOnMainThread(
                () =>
                {
                    superView.AddSubview(view);

                    superView.ConstrainLayout(() => view.Width() == superView.Width() * 2);

                    var constraints = superView.Constraints;
                    Assert.Equal(1, constraints.Length);

                    Assert.Same(view, constraints[0].FirstItem);
                    Assert.Equal(NSLayoutAttribute.Width, constraints[0].FirstAttribute);
                    Assert.Same(superView, constraints[0].SecondItem);
                    Assert.Equal(NSLayoutAttribute.Width, constraints[0].SecondAttribute);
                    Assert.Equal(2f, constraints[0].Multiplier);
                    Assert.Equal(0f, constraints[0].Constant);
                });
        }

        [Fact]
        public void constrain_layout_allows_constraints_with_a_multiplier_via_division_to_be_configured()
        {
            var superView = new UIView();
            var view = new UIView();

            superView.InvokeOnMainThread(
                () =>
                {
                    superView.AddSubview(view);

                    superView.ConstrainLayout(() => view.Width() == superView.Width() / 2);

                    var constraints = superView.Constraints;
                    Assert.Equal(1, constraints.Length);

                    Assert.Same(view, constraints[0].FirstItem);
                    Assert.Equal(NSLayoutAttribute.Width, constraints[0].FirstAttribute);
                    Assert.Same(superView, constraints[0].SecondItem);
                    Assert.Equal(NSLayoutAttribute.Width, constraints[0].SecondAttribute);
                    Assert.Equal(0.5f, constraints[0].Multiplier);
                    Assert.Equal(0f, constraints[0].Constant);
                });
        }

        [Fact]
        public void constrain_layout_allows_constraints_with_a_constant_to_be_configured()
        {
            var superView = new UIView();
            var view = new UIView();

            superView.InvokeOnMainThread(
                () =>
                {
                    superView.AddSubview(view);

                    superView.ConstrainLayout(() => view.Left() == superView.Left() + 20);

                    var constraints = superView.Constraints;
                    Assert.Equal(1, constraints.Length);

                    Assert.Same(view, constraints[0].FirstItem);
                    Assert.Equal(NSLayoutAttribute.Left, constraints[0].FirstAttribute);
                    Assert.Same(superView, constraints[0].SecondItem);
                    Assert.Equal(NSLayoutAttribute.Left, constraints[0].SecondAttribute);
                    Assert.Equal(1f, constraints[0].Multiplier);
                    Assert.Equal(20f, constraints[0].Constant);
                });
        }

        [Fact]
        public void constrain_layout_allows_constraints_with_a_negative_constant_to_be_configured()
        {
            var superView = new UIView();
            var view = new UIView();

            superView.InvokeOnMainThread(
                () =>
                {
                    superView.AddSubview(view);

                    superView.ConstrainLayout(() => view.Left() == superView.Left() - 20);

                    var constraints = superView.Constraints;
                    Assert.Equal(1, constraints.Length);

                    Assert.Same(view, constraints[0].FirstItem);
                    Assert.Equal(NSLayoutAttribute.Left, constraints[0].FirstAttribute);
                    Assert.Same(superView, constraints[0].SecondItem);
                    Assert.Equal(NSLayoutAttribute.Left, constraints[0].SecondAttribute);
                    Assert.Equal(1f, constraints[0].Multiplier);
                    Assert.Equal(-20f, constraints[0].Constant);
                });
        }

        [Fact]
        public void constrain_layout_allows_constraints_with_a_dynamically_evaluated_constant_to_be_configured()
        {
            var someNumber = 50;
            var superView = new UIView();
            var view = new UIView();

            superView.InvokeOnMainThread(
                () =>
                {
                    superView.AddSubview(view);

                    superView.ConstrainLayout(() => view.Left() == superView.Left() + (someNumber * 2));

                    var constraints = superView.Constraints;
                    Assert.Equal(1, constraints.Length);

                    Assert.Same(view, constraints[0].FirstItem);
                    Assert.Equal(NSLayoutAttribute.Left, constraints[0].FirstAttribute);
                    Assert.Same(superView, constraints[0].SecondItem);
                    Assert.Equal(NSLayoutAttribute.Left, constraints[0].SecondAttribute);
                    Assert.Equal(1f, constraints[0].Multiplier);
                    Assert.Equal(100f, constraints[0].Constant);
                });
        }

        [Fact]
        public void constrain_layout_allows_constraints_with_both_a_multiplier_and_constant_to_be_configured()
        {
            var superView = new UIView();
            var view = new UIView();

            superView.InvokeOnMainThread(
                () =>
                {
                    superView.AddSubview(view);

                    superView.ConstrainLayout(() => view.Left() == superView.Left() * 2 + 100);

                    var constraints = superView.Constraints;
                    Assert.Equal(1, constraints.Length);

                    Assert.Same(view, constraints[0].FirstItem);
                    Assert.Equal(NSLayoutAttribute.Left, constraints[0].FirstAttribute);
                    Assert.Same(superView, constraints[0].SecondItem);
                    Assert.Equal(NSLayoutAttribute.Left, constraints[0].SecondAttribute);
                    Assert.Equal(2f, constraints[0].Multiplier);
                    Assert.Equal(100f, constraints[0].Constant);
                });
        }

        [Fact]
        public void constrain_layout_allows_constraints_with_both_a_multiplier_and_negative_constant_to_be_configured()
        {
            var superView = new UIView();
            var view = new UIView();

            superView.InvokeOnMainThread(
                () =>
                {
                    superView.AddSubview(view);

                    superView.ConstrainLayout(() => view.Left() == superView.Left() * 2 - 100);

                    var constraints = superView.Constraints;
                    Assert.Equal(1, constraints.Length);

                    Assert.Same(view, constraints[0].FirstItem);
                    Assert.Equal(NSLayoutAttribute.Left, constraints[0].FirstAttribute);
                    Assert.Same(superView, constraints[0].SecondItem);
                    Assert.Equal(NSLayoutAttribute.Left, constraints[0].SecondAttribute);
                    Assert.Equal(2f, constraints[0].Multiplier);
                    Assert.Equal(-100f, constraints[0].Constant);
                });
        }

        [Fact]
        public void constrain_layout_allows_constraints_with_both_a_dynamic_multiplier_and_dynamic_constant_to_be_configured()
        {
            var someNumber = 5;
            var superView = new UIView();
            var view = new UIView();

            superView.InvokeOnMainThread(
                () =>
                {
                    superView.AddSubview(view);

                    superView.ConstrainLayout(() => view.Left() == superView.Left() * (2 + someNumber) + (someNumber * 10));

                    var constraints = superView.Constraints;
                    Assert.Equal(1, constraints.Length);

                    Assert.Same(view, constraints[0].FirstItem);
                    Assert.Equal(NSLayoutAttribute.Left, constraints[0].FirstAttribute);
                    Assert.Same(superView, constraints[0].SecondItem);
                    Assert.Equal(NSLayoutAttribute.Left, constraints[0].SecondAttribute);
                    Assert.Equal(7f, constraints[0].Multiplier);
                    Assert.Equal(50f, constraints[0].Constant);
                });
        }

        [Fact]
        public void constrain_layout_allows_a_constraint_against_a_constant_only()
        {
            var superView = new UIView();
            var view = new UIView();

            superView.InvokeOnMainThread(
                () =>
                {
                    superView.AddSubview(view);

                    superView.ConstrainLayout(() => view.Width() == 50);

                    var constraints = superView.Constraints;
                    Assert.Equal(1, constraints.Length);

                    Assert.Same(view, constraints[0].FirstItem);
                    Assert.Equal(NSLayoutAttribute.Width, constraints[0].FirstAttribute);
                    Assert.Null(constraints[0].SecondItem);
                    Assert.Equal(NSLayoutAttribute.NoAttribute, constraints[0].SecondAttribute);
                    Assert.Equal(1f, constraints[0].Multiplier);
                    Assert.Equal(50f, constraints[0].Constant);
                });
        }

        [Fact]
        public void constrain_layout_sets_translates_autoresizing_mask_into_constraints_to_false_for_any_subviews_of_the_constrained_view()
        {
            var superView = new UIView();
            var subView1 = new UIView();
            var subView2 = new UIView();

            superView.InvokeOnMainThread(
                () =>
                {
                    superView.AddSubviews(subView1, subView2);

                    superView.ConstrainLayout(() =>
                        subView1.Left() == superView.Left() &&
                        subView2.Left() == superView.Left());

                    Assert.True(superView.TranslatesAutoresizingMaskIntoConstraints);
                    Assert.False(subView1.TranslatesAutoresizingMaskIntoConstraints);
                    Assert.False(subView2.TranslatesAutoresizingMaskIntoConstraints);
                });
        }

        #region Supporting Types

        private class ViewImposter
        {
            public int Left(int someArg)
            {
                return 0;
            }

            public int Right(int first, int second)
            {
                return 0;
            }
        }

        #endregion
    }
}
```