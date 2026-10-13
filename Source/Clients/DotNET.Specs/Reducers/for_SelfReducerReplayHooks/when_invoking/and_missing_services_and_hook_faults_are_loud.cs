// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Reducers.for_SelfReducerReplayHooks.when_invoking;

public class and_missing_services_and_hook_faults_are_loud : Specification
{
    Exception _missing;
    Exception _sync;
    Exception _async;
    IReplayWork _work;
    int _created;
    int _disposed;

    async Task Because()
    {
        _work = Substitute.For<IReplayWork>();
        _work.FailSynchronously().Returns(_ => throw new HookFailed());
        _work.FailAsynchronously().Returns(Task.FromException(new HookFailed()));
        var registry = RegisteredEventTypesForSpecifications.Create((typeof(Created), new("created", 1)));
        var hooks = new SelfReducerReplayHooks(registry, typeof(State));
        await using var missing = new ServiceCollection().BuildServiceProvider();
        _missing = await Catch.Exception(async () => await hooks.Invoke(missing, ReplayState.BeginReplay, "one"));
        await using var provider = new ServiceCollection().AddScoped<IReplayWork>(_ =>
        {
            Interlocked.Increment(ref _created);
            return new DisposableWork(_work, () => Interlocked.Increment(ref _disposed));
        }).BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        _sync = await Catch.Exception(async () => await hooks.Invoke(provider, ReplayState.BeginReplay, "one"));
        _async = await Catch.Exception(async () => await hooks.Invoke(provider, ReplayState.EndReplay, "one"));
    }

    [Fact] void should_reject_a_missing_service_without_default_injection() => _missing.ShouldBeOfExactType<InvalidSelfReducerLifecycleHook>();
    [Fact] void should_unwrap_a_synchronous_hook_fault() => _sync.ShouldBeOfExactType<HookFailed>();
    [Fact] void should_propagate_an_asynchronous_hook_fault() => _async.ShouldBeOfExactType<HookFailed>();
    [Fact] void should_resolve_services_only_at_invocation() => _created.ShouldEqual(2);
    [Fact] void should_dispose_each_failed_notification_scope() => _disposed.ShouldEqual(2);

    public interface IReplayWork
    {
        Task FailSynchronously();
        Task FailAsynchronously();
    }
    public record Created;
    public record State
    {
        public static State Reduce(Created @event, State? current) => new();
        public static Task BeginReplay(IReplayWork work) => work.FailSynchronously();
        public static Task EndReplay(IReplayWork work) => work.FailAsynchronously();
    }
    public class DisposableWork(IReplayWork work, Action disposed) : IReplayWork, IAsyncDisposable
    {
        public Task FailSynchronously() => work.FailSynchronously();
        public Task FailAsynchronously() => work.FailAsynchronously();
        public ValueTask DisposeAsync()
        {
            disposed();
            return ValueTask.CompletedTask;
        }
    }
    public class HookFailed : Exception;
}
