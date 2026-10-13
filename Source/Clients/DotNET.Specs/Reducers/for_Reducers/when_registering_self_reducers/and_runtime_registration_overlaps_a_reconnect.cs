// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Cratis.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_self_reducers;

public class and_runtime_registration_overlaps_a_reconnect : given.all_dependencies
{
    readonly TaskCompletionSource _snapshotEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseSnapshot = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _registrationAttempted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ConnectionLifecycle _lifecycle;
    IReducerHandler _before;
    bool _reconnecting;
    bool _snapshotOwnsMonitor;
    bool _registrationWasExcluded;
    bool _registrationOwnsMonitor;

    void Establish()
    {
        _lifecycle = new(NullLogger<ConnectionLifecycle>.Instance);
        _eventStore.Connection.Lifecycle.Returns(_lifecycle);
        _clientArtifacts.Reducers.Returns([typeof(First)]);
        _eventTypes.AllClrTypes.Returns([typeof(Changed)]);
        _eventTypes.GetEventTypeFor(typeof(Changed)).Returns(new EventType("changed", 1));
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(Observable.Never<ReduceOperationMessage>());
        var naming = Substitute.For<INamingPolicy>();
        naming.GetReadModelName(Arg.Any<Type>()).Returns(call =>
        {
            var type = call.Arg<Type>();
            if (_reconnecting && type == typeof(First))
            {
                _snapshotOwnsMonitor = Monitor.IsEntered(_reducers.ReadModelOwnershipSync);
                _snapshotEntered.TrySetResult();
                _releaseSnapshot.Task.GetAwaiter().GetResult();
            }
            if (type == typeof(Second))
            {
                _registrationOwnsMonitor = Monitor.IsEntered(_reducers.ReadModelOwnershipSync);
            }
            return type.Name;
        });
        _namingPolicy = naming;
        _reducers = CreateReducers();
        _eventStore.Reducers.Returns(_reducers);
    }

    async Task Because()
    {
        await _reducers.Discover();
        _before = _reducers.GetHandlerFor(typeof(First));
        _reconnecting = true;
        var reconnect = Task.Run(_lifecycle.Disconnected);
        await _snapshotEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var registration = Task.Run(async () =>
        {
            var acquired = Monitor.TryEnter(_reducers.ReadModelOwnershipSync);
            _registrationWasExcluded = !acquired;
            if (acquired)
            {
                Monitor.Exit(_reducers.ReadModelOwnershipSync);
            }
            _registrationAttempted.TrySetResult();
            await _reducers.Register(typeof(Second));
        });
        try
        {
            await _registrationAttempted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            _releaseSnapshot.TrySetResult();
        }
        await Task.WhenAll(reconnect, registration).WaitAsync(TimeSpan.FromSeconds(10));
    }

    void Destroy()
    {
        _releaseSnapshot.TrySetResult();
        foreach (var handler in _reducers.GetAllHandlers())
        {
            handler.Disconnect();
            (handler as IDisposable)?.Dispose();
        }
    }

    [Fact] void should_hold_the_ownership_monitor_during_reconnect_replacement() => _snapshotOwnsMonitor.ShouldBeTrue();
    [Fact] void should_exclude_runtime_registration_from_the_reconnect_snapshot() => _registrationWasExcluded.ShouldBeTrue();
    [Fact] void should_hold_the_same_monitor_during_runtime_registration() => _registrationOwnsMonitor.ShouldBeTrue();
    [Fact] void should_keep_the_recreated_and_new_handlers() => _reducers.GetAllHandlers().Select(handler => handler.ReducerType).ShouldContainOnly(typeof(First), typeof(Second));
    [Fact] void should_replace_the_original_handler() => ReferenceEquals(_before, _reducers.GetHandlerFor(typeof(First))).ShouldBeFalse();
    [Fact] void should_keep_both_model_indexes() => (_reducers.HasFor<First>() && _reducers.HasFor<Second>()).ShouldBeTrue();

    [EventType] public record Changed;
    public record First
    {
        public static First Reduce(Changed @event, First? current) => current ?? new();
    }
    public record Second
    {
        public static Second Reduce(Changed @event, Second? current) => current ?? new();
    }
}
