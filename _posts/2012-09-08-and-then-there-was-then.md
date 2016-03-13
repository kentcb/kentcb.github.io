---
title: And then there was Then
assets: /assets/2012-09-08-and-then-there-was-then/
tags: [ ".NET", "TPL" ]
---
In his excellent post [Processing Sequences of Asynchronous Operations with Tasks](http://blogs.msdn.com/b/pfxteam/archive/2010/11/21/10094564.aspx), Stephen Toub discusses how a series of asynchronous operations can be run one after the other in a pre-.NET 4.5 world (a world in which I currently reside, both at work and at home). I won't go into the details here - you should just read his post - but suffice to say that an implementation of a set of `Then` extension methods is desirable as a functional equivalent to the `await` keyword. This allows us to chain together asynchronous operations with ease and with better performance than that attainable with `ContinueWith` on its own:

```csharp
DownloadImageAsync()
    .Then(x => SearchForAliensAsync())
    .Then(x => DistributeResultsAsync());
```

Stephen provides an implementation of `Then` and hints at the usefulness of further overloads. In this post, I provide my own implementation of `Then` that includes all overloads that I think are useful.

Firstly, here are the signatures for my overloads of `Then`:

```csharp
public static Task Then(this Task antecedent, Func<Task, Task> getSuccessor)
public static Task Then(this Task antecedent, Action<Task> successor, TaskCreationOptions taskCreationOptions = TaskCreationOptions.None, TaskScheduler scheduler = null)
 
public static Task<TSuccessor> Then<TSuccessor>(this Task antecedent, Func<Task, Task<TSuccessor>> getSuccessor)
public static Task<TSuccessor> Then<TSuccessor>(this Task antecedent, Func<Task, TSuccessor> successor, TaskCreationOptions taskCreationOptions = TaskCreationOptions.None, TaskScheduler scheduler = null)
 
public static Task Then<TAntecedent>(this Task<TAntecedent> antecedent, Func<Task<TAntecedent>, Task> getSuccessor)
public static Task Then<TAntecedent>(this Task<TAntecedent> antecedent, Action<Task<TAntecedent>> successor, TaskCreationOptions taskCreationOptions = TaskCreationOptions.None, TaskScheduler scheduler = null)
 
public static Task<TSuccessor> Then<TAntecedent, TSuccessor>(this Task<TAntecedent> antecedent, Func<Task<TAntecedent>, Task<TSuccessor>> getSuccessor)
public static Task<TSuccessor> Then<TAntecedent, TSuccessor>(this Task<TAntecedent> antecedent, Func<Task<TAntecedent>, TSuccessor> successor, TaskCreationOptions taskCreationOptions = TaskCreationOptions.None, TaskScheduler scheduler = null)
```

Note that due to the use of optional arguments, there are more overload permutations here than apparent at first glance. Broadly, there are four supported scenarios:

* A non-generic antecedent task THEN a non-generic successor task.
* A non-generic antecedent task THEN a generic successor task.
* A generic antecedent task THEN a non-generic successor task.
* A generic antecedent task THEN a generic successor task.

Each scenario comes in two "flavours". The first flavour requires that the caller provide a `Func` that returns the successor `Task`. The second flavour allows you to specify the task logic in an `Action` or `Func`, which will be automatically wrapped in a `Task` for you.

Four scenarios and two flavours means eight overloads, many of which include optional arguments. The result is a great deal of flexibility in how you use `Then`:

```csharp
InitializeAsync()
    .Then(x => Console.WriteLine("Initialize step done."))                  // an action that is wrapped in a Task for us
    .Then(x => DownloadDataAsync())                                         // a method that returns a Task
    .Then(                                                                  // a func that is wrapped in a Task for us
        x =>
        {
            Console.WriteLine("Download step done: " + x.Result);
            return x.Result;
        })
    .Then(x =>                                                              // a func that returns the Task with which to continue
        {
            if (x.Result.Contains("MAGIC"))
            {
                return ProcessMagicAsync();
            }
            else
            {
                return ProcessNonMagicAsync();
            }
        })
    .Then(x => Console.WriteLine("Processing step done: " + x.Result));     // another action
```

Right, on to the implementation then. To improve maintainability, I really wanted to ensure I had only a single implementation of the core `Then` logic, no matter the number of overloads I made available. This presented a problem in that the core implementation would need to be generic, but then the non-generic overloads would not be able to call it (because their `Task` instances are not generic). To that end, I created a simple method that takes a non-generic `Task` and wraps it as a `Task<bool>`:

```csharp
public static Task<bool> ToBooleanTask(this Task task)
{
    var taskCompletionSource = new TaskCompletionSource<bool>();
 
    task.ContinueWith(t => taskCompletionSource.TrySetException(t.Exception.GetBaseException()), TaskContinuationOptions.OnlyOnFaulted);
    task.ContinueWith(t => taskCompletionSource.TrySetCanceled(), TaskContinuationOptions.OnlyOnCanceled);
    task.ContinueWith(t => taskCompletionSource.TrySetResult(true), TaskContinuationOptions.OnlyOnRanToCompletion);
 
    return taskCompletionSource.Task;
}
```

If and when the non-generic `Task` succeeds, the wrapper `Task<bool>` assumes a result of `true`. If it is cancelled or fails, that cancellation or failure propagates to the wrapper `Task<bool>` too. So now any `Task` can be treated as a `Task<bool>`, thus allowing our non-generic overloads to call into our generic core implementation.

With that in place, I could create the core implementation:

```csharp
private static Task<TSuccessor> ThenImpl<TAntecedent, TSuccessor>(Task<TAntecedent> antecedent, Func<Task<TAntecedent>, Task<TSuccessor>> getSuccessor)
{
    antecedent.AssertNotNull("antecedent");
    getSuccessor.AssertNotNull("getSuccessor");
 
    var taskCompletionSource = new TaskCompletionSource<TSuccessor>();
 
    antecedent.ContinueWith(
        delegate
        {
            if (antecedent.IsFaulted)
            {
                taskCompletionSource.TrySetException(antecedent.Exception.InnerExceptions);
            }
            else if (antecedent.IsCanceled)
            {
                taskCompletionSource.TrySetCanceled();
            }
            else
            {
                try
                {
                    var successorTask = getSuccessor(antecedent);
 
                    if (successorTask == null)
                    {
                        taskCompletionSource.TrySetCanceled();
                    }
                    else
                    {
                        successorTask.ContinueWith(
                            delegate
                            {
                                if (successorTask.IsFaulted)
                                {
                                    taskCompletionSource.TrySetException(successorTask.Exception.InnerExceptions);
                                }
                                else if (successorTask.IsCanceled)
                                {
                                    taskCompletionSource.TrySetCanceled();
                                }
                                else
                                {
                                    taskCompletionSource.TrySetResult(successorTask.Result);
                                }
                            },
                            TaskContinuationOptions.ExecuteSynchronously);
                    }
                }
                catch (Exception ex)
                {
                    taskCompletionSource.TrySetException(ex);
                }
            }
        },
        TaskContinuationOptions.ExecuteSynchronously);
 
    return taskCompletionSource.Task;
}
```

This is very similar to the implementation provided on Stephen's blog, since I used his solution as a starting point.

With these two pieces in place, I could add all the overloads I required:

```csharp
public static Task Then(this Task antecedent, Func<Task, Task> getSuccessor)
{
    Func<Task<bool>, Task<bool>> getSuccessorAsBoolean = x =>
    {
        var successiveTask = getSuccessor(x);
        return successiveTask == null ? null : successiveTask.ToBooleanTask();
    };
    return ThenImpl<bool, bool>(antecedent.ToBooleanTask(), getSuccessorAsBoolean);
}
 
public static Task Then(this Task antecedent, Action<Task> successor, TaskCreationOptions taskCreationOptions = TaskCreationOptions.None, TaskScheduler scheduler = null)
{
    successor.AssertNotNull("successor");
 
    Func<Task<bool>, Task<bool>> getSuccessor = x =>
    {
        var successiveTask = new Task(() => successor(antecedent), taskCreationOptions);
        successiveTask.Start(scheduler ?? TaskScheduler.Default);
        return successiveTask.ToBooleanTask();
    };
    return ThenImpl<bool, bool>(antecedent.ToBooleanTask(), getSuccessor);
}
 
public static Task<TSuccessor> Then<TSuccessor>(this Task antecedent, Func<Task, Task<TSuccessor>> getSuccessor)
{
    return ThenImpl<bool, TSuccessor>(antecedent.ToBooleanTask(), getSuccessor);
}
 
public static Task<TSuccessor> Then<TSuccessor>(this Task antecedent, Func<Task, TSuccessor> successor, TaskCreationOptions taskCreationOptions = TaskCreationOptions.None, TaskScheduler scheduler = null)
{
    successor.AssertNotNull("successor");
 
    Func<Task<bool>, Task<TSuccessor>> getSuccessor = x =>
    {
        var successiveTask = new Task<TSuccessor>(() => successor(antecedent), taskCreationOptions);
        successiveTask.Start(scheduler ?? TaskScheduler.Default);
        return successiveTask;
    };
    return ThenImpl<bool, TSuccessor>(antecedent.ToBooleanTask(), getSuccessor);
}
 
public static Task Then<TAntecedent>(this Task<TAntecedent> antecedent, Func<Task<TAntecedent>, Task> getSuccessor)
{
    Func<Task<TAntecedent>, Task<bool>> getSuccessorAsBoolean = x =>
    {
        var successiveTask = getSuccessor(x);
        return successiveTask == null ? null : successiveTask.ToBooleanTask();
    };
    return ThenImpl<TAntecedent, bool>(antecedent, getSuccessorAsBoolean);
}
 
public static Task Then<TAntecedent>(this Task<TAntecedent> antecedent, Action<Task<TAntecedent>> successor, TaskCreationOptions taskCreationOptions = TaskCreationOptions.None, TaskScheduler scheduler = null)
{
    successor.AssertNotNull("successor");
 
    Func<Task<TAntecedent>, Task<bool>> getSuccessor = x =>
    {
        var successiveTask = new Task(() => successor(antecedent), taskCreationOptions);
        successiveTask.Start(scheduler ?? TaskScheduler.Default);
        return successiveTask.ToBooleanTask();
    };
    return ThenImpl<TAntecedent, bool>(antecedent, getSuccessor);
}
 
public static Task<TSuccessor> Then<TAntecedent, TSuccessor>(this Task<TAntecedent> antecedent, Func<Task<TAntecedent>, Task<TSuccessor>> getSuccessor)
{
    return ThenImpl<TAntecedent, TSuccessor>(antecedent, getSuccessor);
}
 
public static Task<TSuccessor> Then<TAntecedent, TSuccessor>(this Task<TAntecedent> antecedent, Func<Task<TAntecedent>, TSuccessor> successor, TaskCreationOptions taskCreationOptions = TaskCreationOptions.None, TaskScheduler scheduler = null)
{
    successor.AssertNotNull("successor");
 
    Func<Task<TAntecedent>, Task<TSuccessor>> getSuccessor = x =>
    {
        var successiveTask = new Task<TSuccessor>(() => successor(antecedent), taskCreationOptions);
        successiveTask.Start(scheduler ?? TaskScheduler.Default);
        return successiveTask;
    };
    return ThenImpl<TAntecedent, TSuccessor>(antecedent, getSuccessor);
}
```

Each of these overloads directly calls the core `ThenImpl` implementation, massaging any parameters as necessary. It's all reasonably straightforward, so I won't elaborate too much here.

In the interests of completeness, I also wrote these unit tests to validate the implementation:

```csharp
[Fact]
public void to_boolean_task_propagates_failures()
{
    var task = Task.Factory.StartNew(() => { throw new InvalidOperationException("Testing."); });
    var booleanTask = task.ToBooleanTask();
 
    var ex = Assert.Throws<AggregateException>(() => booleanTask.Wait(TimeSpan.FromSeconds(1)));
    Assert.Equal(1, ex.InnerExceptions.Count);
    Assert.IsType<InvalidOperationException>(ex.InnerExceptions[0]);
    Assert.Equal("Testing.", ex.InnerExceptions[0].Message);
}
 
[Fact]
public void to_boolean_task_propagates_cancellation()
{
    var cancellationTokenSource = new CancellationTokenSource();
    cancellationTokenSource.Cancel();
 
    var task = Task.Factory.StartNew(() => cancellationTokenSource.Token.ThrowIfCancellationRequested(), cancellationTokenSource.Token);
    var booleanTask = task.ToBooleanTask();
 
    var ex = Assert.Throws<AggregateException>(() => booleanTask.Wait(TimeSpan.FromSeconds(1)));
    Assert.Equal(1, ex.InnerExceptions.Count);
    Assert.IsType<TaskCanceledException>(ex.InnerExceptions[0]);
}
 
[Fact]
public void to_boolean_task_propagates_success()
{
    var task = Task.Factory.StartNew(() => { });
    var booleanTask = task.ToBooleanTask();
 
    Assert.True(booleanTask.Wait(TimeSpan.FromSeconds(1)));
}
 
[Fact]
public void then_non_generic_second_task_does_not_start_until_first_is_finished()
{
    var executed = false;
 
    var task = Task.Factory
        .StartNew(
            () =>
            {
                Thread.Sleep(100);
                executed = true;
            })
        .Then(
            x =>
            {
                Assert.True(executed);
            });
 
    Assert.True(task.Wait(TimeSpan.FromSeconds(3)));
}
 
[Fact]
public void then_non_generic_fault_in_first_task_prevents_second_task_from_running()
{
    var executed = false;
 
    var task = Task.Factory
        .StartNew(
            () =>
            {
                throw new InvalidOperationException("Failure");
            })
        .Then(
            x =>
            {
                executed = true;
            });
 
    try
    {
        task.Wait(TimeSpan.FromSeconds(3));
        Assert.True(false);
    }
    catch (AggregateException ex)
    {
        Assert.False(executed);
        Assert.Equal(TaskStatus.Faulted, task.Status);
        Assert.Equal(1, ex.InnerExceptions.Count);
    }
}
 
[Fact]
public void then_non_generic_fault_in_second_task_results_in_faulted_overall_task()
{
    var executed = false;
 
    var task = Task.Factory
        .StartNew(() => { })
        .Then(
            x =>
            {
                throw new InvalidOperationException("Failure");
            })
        .Then(
            x =>
            {
                executed = true;
            });
 
    try
    {
        task.Wait(TimeSpan.FromSeconds(3));
        Assert.True(false);
    }
    catch (AggregateException ex)
    {
        Assert.False(executed);
        Assert.Equal(TaskStatus.Faulted, task.Status);
        Assert.Equal(1, ex.InnerExceptions.Count);
    }
}
 
[Fact]
public void then_non_generic_cancellation_in_first_task_prevents_second_task_from_running()
{
    var executed = false;
 
    using (var cancellationTokenSource = new CancellationTokenSource())
    {
        // cancel up-front
        cancellationTokenSource.Cancel();
 
        var cancellationToken = cancellationTokenSource.Token;
 
        var task = Task.Factory
            .StartNew(
                () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                },
                cancellationToken)
            .Then(
                x =>
                {
                    executed = true;
                });
 
        try
        {
            task.Wait(TimeSpan.FromSeconds(3));
            Assert.True(false);
        }
        catch (AggregateException ex)
        {
            Assert.False(executed);
            Assert.Equal(TaskStatus.Canceled, task.Status);
            Assert.Equal(1, ex.InnerExceptions.Count);
        }
    }
}
 
[Fact]
public void then_non_generic_cancellation_in_second_task_results_in_overall_canceled_task()
{
    var executed = false;
 
    using (var cancellationTokenSource = new CancellationTokenSource())
    {
        // cancel up-front
        cancellationTokenSource.Cancel();
 
        var cancellationToken = cancellationTokenSource.Token;
 
        var task = Task.Factory
            .StartNew(() => { }, cancellationToken)
            .Then(
                x =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                })
            .Then(
                x =>
                {
                    executed = true;
                });
 
        try
        {
            task.Wait(TimeSpan.FromSeconds(3));
            Assert.True(false);
        }
        catch (AggregateException ex)
        {
            Assert.False(executed);
            Assert.Equal(TaskStatus.Canceled, task.Status);
            Assert.Equal(1, ex.InnerExceptions.Count);
        }
    }
}
 
[Fact]
public void then_non_generic_cancellation_is_automatic_if_next_task_is_null()
{
    var task = Task.Factory
        .StartNew(() => { })
        .Then(x => null);
 
    try
    {
        task.Wait(TimeSpan.FromSeconds(3));
        Assert.True(false);
    }
    catch (AggregateException ex)
    {
        Assert.Equal(TaskStatus.Canceled, task.Status);
        Assert.Equal(1, ex.InnerExceptions.Count);
    }
}
 
[Fact]
public void then_non_generic_antecedent_task_is_passed_through()
{
    var task1 = Task.Factory
        .StartNew(() => { });
 
    var task2 = task1
        .Then(x => Assert.Same(task1, x));
 
    var task3 = task2
        .Then(x => Assert.Same(task2, x));
 
    Assert.True(task3.Wait(TimeSpan.FromSeconds(2)));
}
 
[Fact]
public void then_generic_second_task_does_not_start_until_first_is_finished()
{
    var executed = false;
 
    var task = Task.Factory
        .StartNew(
            () =>
            {
                Thread.Sleep(100);
                executed = true;
 
                return "result 1";
            })
        .Then(
            x =>
            {
                Assert.True(executed);
 
                return "result 2";
            });
 
    Assert.True(task.Wait(TimeSpan.FromSeconds(3)));
    Assert.Equal("result 2", task.Result);
}
 
[Fact]
public void then_generic_fault_in_first_task_prevents_second_task_from_running()
{
    var executed = false;
 
    var task = Task.Factory
        .StartNew<string>(
            () =>
            {
                throw new InvalidOperationException("Failure");
            })
        .Then(
            x =>
            {
                executed = true;
 
                return "result 2";
            });
 
    try
    {
        task.Wait(TimeSpan.FromSeconds(3));
        Assert.True(false);
    }
    catch (AggregateException ex)
    {
        Assert.False(executed);
        Assert.Equal(TaskStatus.Faulted, task.Status);
        Assert.Equal(1, ex.InnerExceptions.Count);
    }
}
 
[Fact]
public void then_generic_fault_in_second_task_results_in_faulted_overall_task()
{
    var executed = false;
 
    var task = Task.Factory
        .StartNew(() => "result 1")
        .Then(
            x =>
            {
                // dummy test to appease compiler
                if (!executed != executed)
                {
                    throw new InvalidOperationException("Failure");
                }
 
                return "result 2";
            })
        .Then(
            x =>
            {
                executed = true;
                return "result 3";
            });
 
    try
    {
        task.Wait(TimeSpan.FromSeconds(3));
        Assert.True(false);
    }
    catch (AggregateException ex)
    {
        Assert.False(executed);
        Assert.Equal(TaskStatus.Faulted, task.Status);
        Assert.Equal(1, ex.InnerExceptions.Count);
    }
}
 
[Fact]
public void then_generic_cancellation_in_first_task_prevents_second_task_from_running()
{
    var executed = false;
 
    using (var cancellationTokenSource = new CancellationTokenSource())
    {
        // cancel up-front
        cancellationTokenSource.Cancel();
 
        var cancellationToken = cancellationTokenSource.Token;
 
        var task = Task.Factory
            .StartNew(
                () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
 
                    return "result 1";
                },
                cancellationToken)
            .Then(
                x =>
                {
                    executed = true;
                    return "result 2";
                });
 
        try
        {
            task.Wait(TimeSpan.FromSeconds(3));
            Assert.True(false);
        }
        catch (AggregateException ex)
        {
            Assert.False(executed);
            Assert.Equal(TaskStatus.Canceled, task.Status);
            Assert.Equal(1, ex.InnerExceptions.Count);
        }
    }
}
 
[Fact]
public void then_generic_cancellation_in_second_task_results_in_overall_canceled_task()
{
    var executed = false;
 
    using (var cancellationTokenSource = new CancellationTokenSource())
    {
        // cancel up-front
        cancellationTokenSource.Cancel();
 
        var cancellationToken = cancellationTokenSource.Token;
 
        var task = Task.Factory
            .StartNew(() => "result 1", cancellationToken)
            .Then(
                x =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return "result ";
                })
            .Then(
                x =>
                {
                    executed = true;
                    return "result 3";
                });
 
        try
        {
            task.Wait(TimeSpan.FromSeconds(3));
            Assert.True(false);
        }
        catch (AggregateException ex)
        {
            Assert.False(executed);
            Assert.Equal(TaskStatus.Canceled, task.Status);
            Assert.Equal(1, ex.InnerExceptions.Count);
        }
    }
}
 
[Fact]
public void then_generic_cancellation_is_automatic_if_next_task_is_null()
{
    var task = Task.Factory
        .StartNew(() => "result 1")
        .Then<string, string>((Func<Task<string>, Task<string>>)(x => null));
 
    try
    {
        task.Wait(TimeSpan.FromSeconds(3));
        Assert.True(false);
    }
    catch (AggregateException ex)
    {
        Assert.Equal(TaskStatus.Canceled, task.Status);
        Assert.Equal(1, ex.InnerExceptions.Count);
    }
}
 
[Fact]
public void then_generic_antecedent_task_is_passed_through()
{
    var task1 = Task.Factory
        .StartNew(() => "One");
 
    var task2 = task1
        .Then(
            x =>
            {
                Assert.Same(task1, x);
                return "Two";
            });
 
    var task3 = task2
        .Then(
            x =>
            {
                Assert.Same(task2, x);
                return "Three";
            });
 
    Assert.True(task3.Wait(TimeSpan.FromSeconds(2)));
}
 
[Fact]
public void then_generic_tasks_can_change_type()
{
    var task = Task.Factory
        .StartNew(() => "One")
        .Then(
            x =>
            {
                Assert.Equal("One", x.Result);
                return 2;
            })
        .Then(
            x =>
            {
                Assert.Equal(2, x.Result);
                return 3d;
            },
            TaskCreationOptions.None)
        .Then(
            x =>
            {
                Assert.Equal(3d, x.Result);
            });
 
    Assert.True(task.Wait(TimeSpan.FromSeconds(2)));
}
```

Alright, that about wraps it up. I hope it is of use to some of you.