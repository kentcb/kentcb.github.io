---
title: EditorConfig Reference for C# Developers
assets: /assets/2017-07-04-editorconfig-reference-for-c#-developers/
tags: [ ".NET", "C#", ".editorConfig", "EditorConfig", "reference" ]
---
## Preamble

If you've not heard of EditorConfig, it's a platform- and language-agnostic format to describe coding styles. Take a look at the [home page](http://editorconfig.org/) for more information.

Recently, the Roslyn/.NET team added support for EditorConfig to Visual Studio. Consequenly, one can simply drop an _.editorconfig_ file alongside one's solution and have Visual Studio automatically point out any style problems in your code. Configuration is hierarchical, too, so if you place another _.editorconfig_ within a subfolder of your solution, those rules will apply therein, overriding any rules applied at a higher level.

Whilst this is an awesome direction to be taking the platform, there are several problems.

Firstly, there is no solid documentation around the C# (and .NET) configuration options that _.editorconfig_ supports inside Visual Studio. There is scattered information out there, including [this promising looking page](https://docs.microsoft.com/en-us/visualstudio/ide/editorconfig-code-style-settings-reference). However, none of it is complete, and most of it is code rather than documentation.

Secondly, the tooling does not integrate with the compiler as yet. That is, an error will be displayed where relevant, but it won't break your build. This is [acknowledged by the Roslyn team](https://developercommunity.visualstudio.com/content/problem/48804/editorconfig-with-rules-set-to-error-produces-erro.html), and is something that will be addressed by them in the near future.

Thirdly - and this is my opinion - the implementation does not yet offer sufficient control over style. Even with the strictest _.editorconfig_ in place, it's still possible for project collaborators to produce code with wildly inconsistent styling. Partly this is due to options that are missing altogether, and partly it's because some options are present but do not support enforcement via code style options (discussed below). This is unfortunate and is unlikely to ever be fully addressed, but I hope the Roslyn team continue to flesh out the available styling options. In the meantime, something is better than nothing in my book.

This blog post seeks to address the first problem only.

Note that I have uploaded a [sample _.editorconfig_ file](https://github.com/kentcb/EditorConfigReference) to GitHub. This file contains every option key listed within this blog post, assigning values that I like to use by default. However, the intention is to make it easy for other developers to use as a starting point when constructing their _.editorconfig_ files.

## Reference

Following is a list of all supported options in alphabetical order. For each option, I list the key, valid values, and example code showing the effect of different settings (note that I have elided sample code when it is not possible to visually portray the effect, such as with whitespace settings). In addition, an indication is given as to whether code style options are supported for that particular key. If code style options are supported, that means the assigned value should be suffixed with a colon (`:`) and then an indication of what should happen when violations are detected. Valid suffixes are:

* `none` : ignore violations, but use the specified value when generating code
* `suggestion` : violations result only in a suggestion being made to the programmer (via dots under the first two characters of the violation)
* `warning` : violations result in a compiler warning
* `error` : violations result in a compiler error

### csharp_indent_block_contents

Key: `csharp_indent_block_contents`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_indent_block_contents = true
if (foo)
{
    var bar = 42;
}

// csharp_indent_block_contents = false
if (foo)
{
var bar = 42;
}
```

### csharp_indent_braces

Key: `csharp_indent_braces`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_indent_braces = true
public void foo()
    {
    }

// csharp_indent_braces = false
public void foo()
{
}

```

### csharp_indent_case_contents

Key: `csharp_indent_case_contents`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_indent_case_contents = true
switch (foo)
{
    case 42:
        break;
    case 13:
        break;
}

// csharp_indent_case_contents = false
switch (foo)
{
    case 42:
    break;
    case 13:
    break;
}
```

### csharp_indent_labels

Key: `csharp_indent_labels`
<br />
Valid values: `flush_left|one_less_than_current|no_change`
<br />
Supports code style option: no
<br />

```csharp
// csharp_indent_labels = flush_left
public class C
{
    public void M()
    {
foo:
        var x =  42;
    }
}

// csharp_indent_labels = one_less_than_current
public class C
{
    public void M()
    {
    foo:
        var x =  42;
    }
}

// csharp_indent_labels = no_change
public class C
{
    public void M()
    {
        foo:
        var x =  42;
    }
}
```

### csharp_indent_switch_labels

Key: `csharp_indent_switch_labels`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_indent_switch_labels = true
switch (foo)
{
    case 42:
        break;
    case 13:
        break;
}

// csharp_indent_switch_labels = false
switch (foo)
{
case 42:
    break;
case 13:
    break;
}
```

### csharp_new_line_before_catch

Key: `csharp_new_line_before_catch`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_new_line_before_catch = true
try
{
}
catch
{
}

// csharp_new_line_before_catch = false
try
{
} catch
{
}
```

### csharp_new_line_before_else

Key: `csharp_new_line_before_else`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_new_line_before_else = true
if (foo)
{
}
else
{
}

// csharp_new_line_before_else = false
if (foo)
{
} else
{
}
```

### csharp_new_line_before_finally

Key: `csharp_new_line_before_finally`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_new_line_before_finally = true
try
{
}
catch
{
}
finally
{
}

// csharp_new_line_before_finally = false
try
{
}
catch
{
} finally
{
}
```

### csharp_new_line_before_members_in_anonymous_types

Key: `csharp_new_line_before_members_in_anonymous_types`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_new_line_before_members_in_anonymous_types = true
var foo = new 
{
    A = 42,
    B = 13
};

// csharp_new_line_before_members_in_anonymous_types = false
var foo = new 
{
    A = 42, B = 13
};
```

### csharp_new_line_before_members_in_object_initializers

Key: `csharp_new_line_before_members_in_object_initializers`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_new_line_before_members_in_object_initializers = true
var foo = new Foo
{
    A = 42,
    B = 13
};

// csharp_new_line_before_members_in_object_initializers = false
var foo = new Foo
{
    A = 42, B = 13
};
```

### csharp_new_line_before_open_brace

Key: `csharp_new_line_before_open_brace`
<br />
Valid values: `all|accessors|types|methods|properties|indexers|events|anonymous_methods|control_blocks|anonymous_types|object_collection_array_initalizers|lambdas|local_functions`
<br />
Supports code style option: no
<br />

```csharp
// csharp_new_line_before_open_brace = all
public void Foo()
{
    if (foo)
    {
    }
}

// csharp_new_line_before_open_brace = methods
public void Foo()
{
    if (foo) {
    }
}

// csharp_new_line_before_open_brace = methods,control_blocks
public void Foo()
{
    if (foo)
    {
    }
}
```

### csharp_new_line_between_query_expression_clauses
**NOTE**: this option does not appear to have any effect, so I've documented what I believe is the intended effect.

Key: `csharp_new_line_between_query_expression_clauses`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_new_line_between_query_expression_clauses = true
var result = from x in xs
             select x;

// csharp_new_line_between_query_expression_clauses = false
var result = from x in xs select x;
```

### csharp_prefer_braces

Key: `csharp_prefer_braces`
<br />
Valid values: `true|false`
<br />
Supports code style option: yes
<br />

```csharp
// csharp_prefer_braces = true:error
if (foo)
{
    return;
}

// csharp_prefer_braces = false:error
if (foo)
    return;
```

### csharp_prefer_simple_default_expression
**NOTE**: this requires C# 7.1

Key: `csharp_prefer_simple_default_expression`
<br />
Valid values: `true|false`
<br />
Supports code style option: yes
<br />

```csharp
// csharp_prefer_simple_default_expression = true:error
public void Foo(int? i = default);

// csharp_prefer_simple_default_expression = false:error
public void Foo(int? i = default(int));
```

### csharp_preserve_single_line_blocks

Key: `csharp_preserve_single_line_blocks`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_preserve_single_line_blocks = true
{ DoSomething(); }

// csharp_preserve_single_line_blocks = false
{
    DoSomething();
}
```

### csharp_preserve_single_line_statements

Key: `csharp_preserve_single_line_statements`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_preserve_single_line_statements = true
if (true) DoSomething();

// csharp_preserve_single_line_statements = false
if (true)
    DoSomething();
```

### csharp_space_after_cast

Key: `csharp_space_after_cast`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_space_after_cast = true
var foo = (int) bar;

// csharp_space_after_cast = false
var foo = (int)bar;
```

### csharp_space_after_colon_in_inheritance_clause

Key: `csharp_space_after_colon_in_inheritance_clause`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_space_after_colon_in_inheritance_clause = true
public class Foo : Bar

// csharp_space_after_colon_in_inheritance_clause = false
public class Foo :Bar
```

### csharp_space_after_comma

Key: `csharp_space_after_comma`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_space_after_comma = true
var foo = new[] { 1, 2, 3 };

// csharp_space_after_comma = false
var foo = new[] { 1,2,3 };
```

### csharp_space_after_dot

Key: `csharp_space_after_dot`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_space_after_dot = true
this. Foo();

// csharp_space_after_dot = false
this.Foo();
```

### csharp_space_after_keywords_in_control_flow_statements

Key: `csharp_space_after_keywords_in_control_flow_statements`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_space_after_keywords_in_control_flow_statements = true
if (foo)
{
}

while (foo)
{
}

// csharp_space_after_keywords_in_control_flow_statements = false
if(foo)
{
}

while(foo)
{
}
```

### csharp_space_after_semicolon_in_for_statement

Key: `csharp_space_after_semicolon_in_for_statement`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_space_after_semicolon_in_for_statement = true
for (var foo = 0; foo < 10; ++foo)
{
}

// csharp_space_after_semicolon_in_for_statement = false
for (var foo = 0;foo < 10;++foo)
{
}
```

### csharp_space_around_binary_operators 
**NOTE**: currently thwarted by [this bug](https://github.com/dotnet/roslyn/issues/20355).

Key: `csharp_space_around_binary_operators `
<br />
Valid values: `before_and_after|ignore|none`
<br />
Supports code style option: no
<br />

```csharp
// csharp_space_around_binary_operators  = before_and_after
var foo = 42 + 42;

// csharp_space_around_binary_operators  = ignore
var foo = 42+   42;

// csharp_space_around_binary_operators  = none
var foo = 42+42;
```

### csharp_space_around_declaration_statements

Key: `csharp_space_around_declaration_statements`
<br />
Valid values: `ignore|do_not_ignore`
<br />
Supports code style option: no
<br />

```csharp
// csharp_space_around_declaration_statements = ignore
var    foo    =    42;

// csharp_space_around_declaration_statements = do_not_ignore
var foo = 42;
```

### csharp_space_before_colon_in_inheritance_clause

Key: `csharp_space_before_colon_in_inheritance_clause`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_space_before_colon_in_inheritance_clause = true
public class Foo : Bar

// csharp_space_before_colon_in_inheritance_clause = false
public class Foo: Bar
```

### csharp_space_before_comma

Key: `csharp_space_before_comma`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_space_before_comma = true
var foo = new[] { 1 , 2 , 3 };

// csharp_space_before_comma = false
var foo = new[] { 1, 2, 3 };
```

### csharp_space_before_dot

Key: `csharp_space_before_dot`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_space_before_dot = true
this .Foo();

// csharp_space_before_dot = false
this.Foo();
```

### csharp_space_before_open_square_brackets

Key: `csharp_space_before_open_square_brackets`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_space_before_open_square_brackets = true
var foo = bar [42];

// csharp_space_before_open_square_brackets = false
var foo = bar[42];
```

### csharp_space_before_semicolon_in_for_statement

Key: `csharp_space_before_semicolon_in_for_statement`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_space_before_semicolon_in_for_statement = true
for (var foo = 0 ; foo < 10 ; ++foo)
{
}

// csharp_space_before_semicolon_in_for_statement = false
for (var foo = 0; foo < 10; ++foo)
{
}
```

### csharp_space_between_empty_square_brackets

Key: `csharp_space_between_empty_square_brackets`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_space_between_empty_square_brackets = true
var foo = new [ ] { 42, 13 };

// csharp_space_between_empty_square_brackets = false
var foo = new [] { 42, 13 };
```

### csharp_space_between_method_call_empty_parameter_list_parentheses

Key: `csharp_space_between_method_call_empty_parameter_list_parentheses`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_space_between_method_call_empty_parameter_list_parentheses = true
this.Foo( );

// csharp_space_between_method_call_empty_parameter_list_parentheses = false
this.Foo();
```

### csharp_space_between_method_call_name_and_opening_parenthesis

Key: `csharp_space_between_method_call_name_and_opening_parenthesis`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_space_between_method_call_name_and_opening_parenthesis = true
this.Foo ();

// csharp_space_between_method_call_name_and_opening_parenthesis = false
this.Foo();
```

### csharp_space_between_method_call_parameter_list_parentheses

Key: `csharp_space_between_method_call_parameter_list_parentheses`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_space_between_method_call_parameter_list_parentheses = true
this.Foo( 42 );

// csharp_space_between_method_call_parameter_list_parentheses = false
this.Foo(42);
```

### csharp_space_between_method_declaration_empty_parameter_list_parentheses

Key: `csharp_space_between_method_declaration_empty_parameter_list_parentheses`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_space_between_method_declaration_empty_parameter_list_parentheses = true
public void Foo( )
{
}

// csharp_space_between_method_declaration_empty_parameter_list_parentheses = false
public void Foo()
{
}
```

### csharp_space_between_method_declaration_name_and_open_parenthesis

Key: `csharp_space_between_method_declaration_name_and_open_parenthesis`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_space_between_method_declaration_name_and_open_parenthesis = true
public void Foo ()
{
}

// csharp_space_between_method_declaration_name_and_open_parenthesis = false
public void Foo()
{
}
```

### csharp_space_between_method_declaration_parameter_list_parentheses

Key: `csharp_space_between_method_declaration_parameter_list_parentheses`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_space_between_method_declaration_parameter_list_parentheses = true
public void Foo( int bar )
{
}

// csharp_space_between_method_declaration_parameter_list_parentheses = false
public void Foo(int bar)
{
}
```

### csharp_space_between_parentheses

Key: `csharp_space_between_parentheses`
<br />
Valid values: `none|expressions|type_casts|control_flow_statements`
<br />
Supports code style option: no
<br />

```csharp
// csharp_space_between_parentheses = none
var foo = (int)bar;

while (foo < 42)
{
    ++foo;
}

// csharp_space_between_parentheses = type_casts
var foo = ( int )bar;

while (foo < 42)
{
    ++foo;
}

// csharp_space_between_parentheses = type_casts,control_flow_statements
var foo = ( int )bar;

while ( foo < 42 )
{
    ++foo;
}
```

### csharp_space_between_square_brackets

Key: `csharp_space_between_square_brackets`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// csharp_space_between_square_brackets = true
foo[ 42 ] = 13;

// csharp_space_between_square_brackets = false
foo[42] = 13;
```

### csharp_style_conditional_delegate_call

Key: `csharp_style_conditional_delegate_call`
<br />
Valid values: `true|false`
<br />
Supports code style option: yes
<br />

```csharp
// csharp_style_conditional_delegate_call = true:error
this.foo?.Invoke(bar);

// csharp_style_conditional_delegate_call = false:error
if (this.foo != null)
{
    this.foo(bar);
}
```

### csharp_style_expression_bodied_accessors

**NOTE**: the `when_on_single_line` value does not appear to work.

Key: `csharp_style_expression_bodied_accessors`
<br />
Valid values: `true|false|when_on_single_line`
<br />
Supports code style option: yes
<br />

```csharp
// csharp_style_expression_bodied_accessors = true:error
private int foo;
public int Foo
{
    get => this.foo;
    set => this.foo = value;
}

// csharp_style_expression_bodied_accessors = false:error
private int foo;
public int Foo
{
    get { return this.foo; }
    set { this.foo = value; }
}
```

### csharp_style_expression_bodied_constructors

**NOTE**: the `when_on_single_line` value does not appear to work.

Key: `csharp_style_expression_bodied_constructors`
<br />
Valid values: `true|false|when_on_single_line`
<br />
Supports code style option: yes
<br />

```csharp
// csharp_style_expression_bodied_constructors = true:error
public Foo() => this.foo = 42;

// csharp_style_expression_bodied_constructors = false:error
public Foo()
{
    this.foo = 42;
}
```

### csharp_style_expression_bodied_indexers

**NOTE**: the `when_on_single_line` value does not appear to work.

Key: `csharp_style_expression_bodied_indexers`
<br />
Valid values: `true|false|when_on_single_line`
<br />
Supports code style option: yes
<br />

```csharp
// csharp_style_expression_bodied_indexers = true:error
public int this[int i] => 42;

// csharp_style_expression_bodied_indexers = false:error
public int this[int i]
{
    get { return 42; }
}
```

### csharp_style_expression_bodied_methods

**NOTE**: the `when_on_single_line` value does not appear to work.

Key: `csharp_style_expression_bodied_methods`
<br />
Valid values: `true|false|when_on_single_line`
<br />
Supports code style option: yes
<br />

```csharp
// csharp_style_expression_bodied_methods = true:error
public int Foo() => 42;

// csharp_style_expression_bodied_methods = false:error
public int Foo()
{
    return 42;
}
```

### csharp_style_expression_bodied_operators

**NOTE**: the `when_on_single_line` value does not appear to work.

Key: `csharp_style_expression_bodied_operators`
<br />
Valid values: `true|false|when_on_single_line`
<br />
Supports code style option: yes
<br />

```csharp
// csharp_style_expression_bodied_operators = true:error
public static int operator +(Foo first, Foo second) => first.Bar + second.Bar;

// csharp_style_expression_bodied_operators = false:error
public static int operator +(Foo first, Foo second)
{
    return first.Bar + second.Bar;
}
```

### csharp_style_expression_bodied_properties

**NOTE**: the `when_on_single_line` value does not appear to work.

Key: `csharp_style_expression_bodied_properties`
<br />
Valid values: `true|false|when_on_single_line`
<br />
Supports code style option: yes
<br />

```csharp
// csharp_style_expression_bodied_properties = true:error
public int Foo => 42;

// csharp_style_expression_bodied_properties = false:error
public int Foo
{
    get { return 42; }
}
```

### csharp_style_inlined_variable_declaration

Key: `csharp_style_inlined_variable_declaration`
<br />
Valid values: `true|false`
<br />
Supports code style option: yes
<br />

```csharp
// csharp_style_inlined_variable_declaration = true:error
if (this.TryParseFoo(out var foo))
{
}

// csharp_style_inlined_variable_declaration = false:error
int foo;

if (this.TryParseFoo(out foo))
{
}
```

### csharp_style_pattern_matching_over_as_with_null_check

Key: `csharp_style_pattern_matching_over_as_with_null_check`
<br />
Valid values: `true|false`
<br />
Supports code style option: yes
<br />

```csharp
// csharp_style_pattern_matching_over_as_with_null_check = true:error
if (foo is string s)
{
}

// csharp_style_pattern_matching_over_as_with_null_check = false:error
var s = foo as string;

if (s != null)
{
}
```

### csharp_style_pattern_matching_over_is_with_cast_check

Key: `csharp_style_pattern_matching_over_is_with_cast_check`
<br />
Valid values: `true|false`
<br />
Supports code style option: yes
<br />

```csharp
// csharp_style_pattern_matching_over_is_with_cast_check = true:error
if (foo is int i)
{
}

// csharp_style_pattern_matching_over_is_with_cast_check = false:error
if (foo is int)
{
    var i = (int)foo;
}
```

### csharp_style_throw_expression

Key: `csharp_style_throw_expression`
<br />
Valid values: `true|false`
<br />
Supports code style option: yes
<br />

```csharp
// csharp_style_throw_expression = true:error
this.foo = bar ?? throw new ArgumentNullException(nameof(bar));

// csharp_style_throw_expression = false:error
if (bar == null)
{
    throw new ArgumentNullException(nameof(bar));
}

this.foo = bar;
```

### csharp_style_var_elsewhere

Key: `csharp_style_var_elsewhere`
<br />
Valid values: `true|false`
<br />
Supports code style option: yes
<br />

```csharp
// csharp_style_var_elsewhere = true:error
int foo = this.Foo();

// csharp_style_var_elsewhere = false:error
var foo = this.Foo();
```

### csharp_style_var_for_built_in_types

Key: `csharp_style_var_for_built_in_types`
<br />
Valid values: `true|false`
<br />
Supports code style option: yes
<br />

```csharp
// csharp_style_var_for_built_in_types = true:error
var foo = 42;

// csharp_style_var_for_built_in_types = false:error
int foo = 42;
```

### csharp_style_var_when_type_is_apparent

Key: `csharp_style_var_when_type_is_apparent`
<br />
Valid values: `true|false`
<br />
Supports code style option: yes
<br />

```csharp
// csharp_style_var_when_type_is_apparent = true:error
var foo = new List<int>();

// csharp_style_var_when_type_is_apparent = false:error
List<int> foo = new List<int>();
```

### dotnet_sort_system_directives_first

Key: `dotnet_sort_system_directives_first`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

```csharp
// dotnet_sort_system_directives_first = true
using System;
using Foo;

// dotnet_sort_system_directives_first = false
using Foo;
using System;
```

### dotnet_style_coalesce_expression

Key: `dotnet_style_coalesce_expression`
<br />
Valid values: `true|false`
<br />
Supports code style option: yes
<br />

```csharp
// dotnet_style_coalesce_expression = true:error
var foo = bar ?? baz;

// dotnet_style_coalesce_expression = false:error
var foo = bar != null ? bar : baz;
```

### dotnet_style_collection_initializer

Key: `dotnet_style_collection_initializer`
<br />
Valid values: `true|false`
<br />
Supports code style option: yes
<br />

```csharp
// dotnet_style_collection_initializer = true:error
var foos = new List<int> { 42, 13 };

// dotnet_style_collection_initializer = false:error
var foo = new List<int>();
foo.Add(42);
foo.Add(13);
```

### dotnet_style_explicit_tuple_names

Key: `dotnet_style_explicit_tuple_names`
<br />
Valid values: `true|false`
<br />
Supports code style option: yes
<br />

```csharp
// dotnet_style_explicit_tuple_names = true:error
(int foo, int bar) data = GetData();
var foo = data.foo;

// dotnet_style_explicit_tuple_names = false:error
(int foo, int bar) data = GetData();
var foo = data.Item1;
```

### dotnet_style_null_propagation

Key: `dotnet_style_null_propagation`
<br />
Valid values: `true|false`
<br />
Supports code style option: yes
<br />

```csharp
// dotnet_style_null_propagation = true:error
var foo = foo?.GetSomething();

// dotnet_style_null_propagation = false:error
var foo = foo == null ? null : foo.GetSomething();
```

### dotnet_style_object_initializer

Key: `dotnet_style_object_initializer`
<br />
Valid values: `true|false`
<br />
Supports code style option: yes
<br />

```csharp
// dotnet_style_object_initializer = true:error
var foo = new Foo
{
    Value = 42
};

// dotnet_style_object_initializer = false:error
var foo = new Foo();
foo.Value = 42;
```

### dotnet_style_predefined_type_for_locals_parameters_members

Key: `dotnet_style_predefined_type_for_locals_parameters_members`
<br />
Valid values: `true|false`
<br />
Supports code style option: yes
<br />

```csharp
// dotnet_style_predefined_type_for_locals_parameters_members = true:error
public void Foo(int bar)
{
}

// dotnet_style_predefined_type_for_locals_parameters_members = false:error
public void Foo(Int32 bar)
{
}
```

### dotnet_style_predefined_type_for_member_access

Key: `dotnet_style_predefined_type_for_member_access`
<br />
Valid values: `true|false`
<br />
Supports code style option: yes
<br />

```csharp
// dotnet_style_predefined_type_for_member_access = true:error
var foo = int.MinValue;

// dotnet_style_predefined_type_for_member_access = false:error
var foo = Int32.MinValue;
```

### dotnet_style_qualification_for_event

Key: `dotnet_style_qualification_for_event`
<br />
Valid values: `true|false`
<br />
Supports code style option: yes
<br />

```csharp
// dotnet_style_qualification_for_event = true:error
this.FooHappened += OnFoo;

// dotnet_style_qualification_for_event = false:error
FooHappened += OnFoo;
```

### dotnet_style_qualification_for_field

Key: `dotnet_style_qualification_for_field`
<br />
Valid values: `true|false`
<br />
Supports code style option: yes
<br />

```csharp
// dotnet_style_qualification_for_field = true:error
this.foo = 42;

// dotnet_style_qualification_for_field = false:error
foo = 42;
```

### dotnet_style_qualification_for_method

Key: `dotnet_style_qualification_for_method`
<br />
Valid values: `true|false`
<br />
Supports code style option: yes
<br />

```csharp
// dotnet_style_qualification_for_method = true:error
this.Foo();

// dotnet_style_qualification_for_method = false:error
Foo();
```

### dotnet_style_qualification_for_property

Key: `dotnet_style_qualification_for_property`
<br />
Valid values: `true|false`
<br />
Supports code style option: yes
<br />

```csharp
// dotnet_style_qualification_for_property = true:error
this.Foo = 42;

// dotnet_style_qualification_for_property = false:error
Foo = 42;
```

### end_of_line

Key: `end_of_line`
<br />
Valid values: `lf|cr|crlf`
<br />
Supports code style option: no
<br />

### indent_size

Key: `indent_size`
<br />
Valid values: `(any integer)`
<br />
Supports code style option: no
<br />

```csharp
// indent_size = 4
if (foo)
{
    var bar = 42;
}

// indent_size = 2
if (foo)
{
  var bar = 42;
}
```

### indent_style

Key: `indent_style`
<br />
Valid values: `space|tab`
<br />
Supports code style option: no
<br />

### insert_final_newline

Key: `insert_final_newline`
<br />
Valid values: `true|false`
<br />
Supports code style option: no
<br />

### tab_width

Key: `tab_width`
<br />
Valid values: `(any integer)`
<br />
Supports code style option: no
<br />