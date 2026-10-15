// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Observation;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

public class and_a_background_attempt_reconnects_to_a_restarted_kernel : given.an_event_store_with_identity_drift
{
    TaskCompletionSource _retryStarted;
    TaskCompletionSource _releaseRetry;
    TaskCompletionSource _callbackJoined;
    int _attempts;

    async Task Establish()
    {
        _retryStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _releaseRetry = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _callbackJoined = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await _connectionLifecycle.Connected();
        var disconnected = typeof(EventStore).GetMethod("MarkRegistrationAfterDisconnect", BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate<Disconnected>(_eventStore);
        _connectionLifecycle.OnDisconnected += disconnected;
        _connectionLifecycle.OnConnected += () =>
        {
            var run = _eventStore.RegisterAll();
            _callbackJoined.TrySetResult();
            return run;
        };
        _eventStore.Seeding.Register().Returns(_ =>
        {
            switch (++_attempts)
            {
                case 1: return Task.FromException(new KernelUnavailable());
                case 2:
                    _retryStarted.TrySetResult();
                    return _releaseRetry.Task;
                default: return Task.CompletedTask;
            }
        });
    }

    async Task Because()
    {
        _exception = await Catch.Exception(_eventStore.RegisterAll);
        var background = _eventStore.PendingBackgroundRegistrationRetry!;
        await _retryStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _servicesAccessor.Services.Observers.GetObservers(Arg.Any<AllObserversRequest>(), Arg.Any<CallContext>()).Returns([]);
        await _connectionLifecycle.Disconnected();
        var reconnect = _connectionLifecycle.Connected();
        await _callbackJoined.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _releaseRetry.SetResult();
        await Task.WhenAll(background, reconnect).WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_surface_the_original_transport_failure() => _exception.ShouldBeOfExactType<KernelUnavailable>();
    [Fact] void should_rerun_the_background_attempt_for_the_new_kernel() => _attempts.ShouldEqual(3);
    [Fact] void should_take_a_fresh_observer_snapshot_after_disconnect() => _servicesAccessor.Services.Observers.Received(2).GetObservers(Arg.Any<AllObserversRequest>(), Arg.Any<CallContext>());
    [Fact] void should_drop_identity_drift_not_present_on_the_restarted_kernel() => _eventStore.Registration.IdentityDrift.ShouldBeEmpty();
    [Fact] void should_clear_registering_after_the_new_run() => _eventStore.Registration.IsRegistering.ShouldBeFalse();

    public class KernelUnavailable() : Exception("Kernel unavailable");
}
