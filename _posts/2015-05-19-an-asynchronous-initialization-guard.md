---
title: An Asynchronous Initialization Guard
assets: /assets/2015-05-19-an-asynchronous-initialization-guard/
tags: [ ".NET", "TPL", "asynchronous", "utility" ]
---
Consider the situation in which a service or view model requires initialization, and that initialization is asynchronous in nature. Perhaps you're initializing a service that in turn needs to interact with a data store of some description. This creates a couple of problems.

Components that employ traditional synchronous initialization can be considered either initialized or uninitialized, but components with asynchronous initialization require a third state: initializing. That is, there is a period of time in which the initialization has been instigated, but has not yet completed. For synchronous initialization we would just block during this (presumably much smaller) period, but for asynchronous initialization we do not have this luxury.

Due to the potential lag between initialization instigation and initialization completion, it becomes even more critical to protect from race conditions whereby multiple attempts to initialize the component results in multiple executions of the initialization logic.

Having fought through the problems of asynchronous initialization several times in different projects, I decided to write an abstraction around it. I called it `InitializationGuard` (my namespace suffices to make it obviously asynchronous in nature, but you might want to call yours `AsyncInitializationGuard` or similar). The public API looks like this (full code is included at the end of this post):

```C#
public enum InitializationGuardState
{
    Uninitialized,
    Initializing,
    Initialized
}

public sealed class InitializationGuard
{
    public InitializationGuard(Func<Task> taskFactory);

    public InitializationGuard(Func<CancellationToken, Task> taskFactory);

    public InitializationGuardState State { get; }

    public Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken));

    public void EnsureInitialized();
}
```

To construct an `InitializationGuard` one must provide a factory with which initialization logic can be instigated. That is, the `taskFactory` parameter is a `Func` that returns a `Task` representing the (already executing) asynchronous initialization logic. The overload allows that `Func` to take a `CancellationToken` for those situations where your initialization logic should support cancellation.

The `State` property yields a snapshot of the current state of the `InitializationGuard`, as per the `InitializationGuardState` enumeration. In particular, notice the presence of the `Initializing` enumeration member.

Client code can invoke `InitializeAsync` to kick off initialization. Any number of threads can be calling this method as spuriously as they like, and only one initialization attempt will be made (barring errors, which I'll discuss momentarily).

Finally, the `EnsureInitialized` method makes it easy for client code to throw an exception if initialization has not completed.

The pattern for typical client code is:

```C#
public class SomeService
{
    private readonly InitializationGuard initializationGuard;

    public SomeService()
    {
        this.initializationGuard = new InitializationGuard(this.InitializeCoreAsync);
    }

    public void SomeOperation()
    {
        this.initializationGuard.EnsureInitialized();

        // do the operation
    }

    public async Task SomeOperationAsync()
    {
        this.initializationGuard.EnsureInitialized();

        // do the operation
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        await this.initializationGuard.InitializeAsync(cancellationToken);
    }

    private async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        // do whatever init work is required here, awaiting as necessary
    }
}
```
 
Here we construct our `InitializationGuard` and tell it to invoke `InitializeCoreAsync` when initialization should be instigated. Inside `InitializeCoreAsync` we perform our initialization logic, which might involve awaiting any number of asynchronous operations.

`SomeOperation` and `SomeOperationAsync` just demonstrates the use of `EnsureInitialized` to guard against the case where clients of the service are neglecting to initialize it first. 

To allow clients to request that initialization, we expose an `InitializeAsync` method. Where this initialization is performed is application-specific. In my mobile apps, I tend to have the startup logic asynchronously initialize any services that require it. Thus, the application won't even attempt to create other dependent objects (such as view models) until all services are properly initialized.

That brings us to the problem of initialization errors. When an error occurs during initialization, `InitializationGuard` reverts its state to `Uninitialized` and re-throws the exception. Thus, any client code awaiting the initialization will receive the exception and has the opportunity to deal with it (perhaps by retrying - it really depends on whether your initialization logic is *expected* to fail periodically or not).

Importantly, this means that it *is* possible for spurious invocations of `InitializeAsync` to result in multiple executions of initialization logic. However, those invocations will always be serialized and only initialization failures will result in the opportunity for multiple invocations to occur.

So there you have it - a useful helper class that allows you to implement asynchronous initialization and protect yourself from some of the risks involved. The full code (including tests) follows.

```C#
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Kent.Boogaart.HelperTrinity.Extensions;

[DebuggerDisplay("{State}")]
public sealed class InitializationGuard
{
    private readonly Func<CancellationToken, Task> taskFactory;
    private readonly object sync;
    private Task initializeTask;
    private volatile InitializationGuardState state;

    public InitializationGuard(Func<Task> taskFactory)
        : this(CreateCancellableTaskFactoryFrom(taskFactory))
    {
    }

    public InitializationGuard(Func<CancellationToken, Task> taskFactory)
    {
        taskFactory.AssertNotNull(nameof(taskFactory));

        this.taskFactory = taskFactory;
        this.sync = new object();
    }

    public InitializationGuardState State => this.state;

    public Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        if (this.state == InitializationGuardState.Initialized)
        {
            return Task.FromResult(true);
        }

        return this.GetOrCreateInitializeTask(cancellationToken);
    }

    public void EnsureInitialized()
    {
        if (this.state != InitializationGuardState.Initialized)
        {
            throw new InitializationException("Not yet initialized.");
        }
    }

    private Task GetOrCreateInitializeTask(CancellationToken cancellationToken)
    {
        var initializeTask = this.initializeTask;

        if (initializeTask != null)
        {
            return initializeTask;
        }

        lock (this.sync)
        {
            initializeTask = this.initializeTask;

            if (initializeTask != null)
            {
                return initializeTask;
            }

            this.initializeTask =  this.CreateInitializeTask(cancellationToken);
            return this.initializeTask;
        }
    }

    private async Task CreateInitializeTask(CancellationToken cancellationToken)
    {
        this.state = InitializationGuardState.Initializing;

        try
        {
            await this.taskFactory(cancellationToken);
            this.state = InitializationGuardState.Initialized;
        }
        catch (Exception)
        {
            this.state = InitializationGuardState.Uninitialized;
            throw;
        }
    }

    private static Func<CancellationToken, Task> CreateCancellableTaskFactoryFrom(Func<Task> nonCancellableTaskFactory)
    {
        if (nonCancellableTaskFactory == null)
        {
            return null;
        }

        return ct => nonCancellableTaskFactory();
    }
}


using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public sealed class InitializationGuardFixture
{
    [Fact]
    public void ctor_requires_initialize_task_factory()
    {
        Assert.Throws<ArgumentNullException>(() => new InitializationGuard((Func<Task>)null));
    }

    [Fact]
    public async Task initialization_only_occurs_once_for_multiple_synchronous_initialization_attempts()
    {
        var count = 0;
        var sut = new InitializationGuard(
            () =>
            {
                ++count;
                return Task.FromResult(true);
            });

        await sut.InitializeAsync();
        Assert.Equal(1, count);

        await sut.InitializeAsync();
        await sut.InitializeAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task initialization_only_occurs_once_for_multiple_asynchronous_initialization_attempts()
    {
        var count = 0;
        var sut = new InitializationGuard(
            () =>
            {
                ++count;
                return Task.FromResult(true);
            });

        await Task.WhenAll(
            sut.InitializeAsync(),
            sut.InitializeAsync(),
            sut.InitializeAsync(),
            sut.InitializeAsync());

        Assert.Equal(1, count);
    }

    [Fact]
    public void an_uninitialized_guard_has_a_state_of_uninitialized()
    {
        var sut = new InitializationGuard(() => Task.FromResult(true));
        Assert.Equal(InitializationGuardState.Uninitialized, sut.State);
    }

    [Fact]
    public void an_initializing_guard_has_a_state_of_initializing()
    {
        var tcs = new TaskCompletionSource<bool>();
        var sut = new InitializationGuard(() => tcs.Task);

        sut.InitializeAsync();
        Assert.Equal(InitializationGuardState.Initializing, sut.State);
    }

    [Fact]
    public async Task an_initialized_guard_has_a_state_of_initialized()
    {
        var sut = new InitializationGuard(() => Task.FromResult(true));

        await sut.InitializeAsync();
        Assert.Equal(InitializationGuardState.Initialized, sut.State);
    }

    [Fact]
    public async Task state_resets_to_uninitialized_if_initialization_is_cancelled()
    {
        var cts = new CancellationTokenSource();
        var sut = new InitializationGuard(() => Task.Run(() => cts.Token.ThrowIfCancellationRequested()));
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await sut.InitializeAsync(cts.Token));
        Assert.Equal(InitializationGuardState.Uninitialized, sut.State);
    }

    [Fact]
    public async Task state_resets_to_uninitialized_if_initialization_fails()
    {
        var sut = new InitializationGuard(() => Task.Run(() => { throw new InvalidOperationException(); }));

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.InitializeAsync());
        Assert.Equal(InitializationGuardState.Uninitialized, sut.State);
    }

    [Fact]
    public async Task ensure_initialized_succeeds_if_initialized()
    {
        var sut = new InitializationGuard(() => Task.FromResult(true));
        await sut.InitializeAsync();

        sut.EnsureInitialized();
    }

    [Fact]
    public async Task error_propagates_if_initialization_is_cancelled()
    {
        var cts = new CancellationTokenSource();
        var sut = new InitializationGuard(() => Task.Run(() => cts.Token.ThrowIfCancellationRequested()));
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await sut.InitializeAsync(cts.Token));
    }

    [Fact]
    public async Task error_propagates_if_initialization_fails()
    {
        var sut = new InitializationGuard(() => Task.Run(() => { throw new InvalidOperationException("Whatever"); }));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.InitializeAsync());
        Assert.Equal("Whatever", ex.Message);
    }

    [Fact]
    public void ensure_initialized_fails_if_not_yet_initialized()
    {
        var sut = new InitializationGuard(() => Task.FromResult(true));
        var ex = Assert.Throws<InitializationException>(() => sut.EnsureInitialized());
        Assert.Equal("Not yet initialized.", ex.Message);
    }
}
```