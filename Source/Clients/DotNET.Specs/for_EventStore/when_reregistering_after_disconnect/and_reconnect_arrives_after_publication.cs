// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_reregistering_after_disconnect;

public class and_reconnect_arrives_after_publication : given.a_registered_store
{
    TaskCompletionSource _published;
    TaskCompletionSource _release;
    TaskCompletionSource _callbackJoined;
    bool _beforeDisconnect;
    Task _run;
    Task _joined;
    int _flights;

    void Establish()
    {
        _published = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _callbackJoined = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var register = typeof(EventStore).GetMethod("RegisterAllCore", BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate<Func<Task>>(_store);
        var flight = new SingleFlightRegistration(async () =>
        {
            await register();
            if (Interlocked.Increment(ref _flights) == 1)
            {
                // Hold the actual completed registration at the single-flight release boundary.
                _published.TrySetResult();
                await _release.Task;
            }
        });
        typeof(EventStore).GetField("_registerAllFlight", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_store, flight);
        _connection.Lifecycle.OnConnected -= _store.RegisterAfterConnect;
        _connection.Lifecycle.OnConnected += () =>
        {
            _joined = _store.RegisterAll();
            _callbackJoined.TrySetResult();
            return _joined;
        };
    }

    async Task Because()
    {
        _run = _store.RegisterAll();
        await _published.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _beforeDisconnect = _store.Registration.IsRegistering;
        await _connection.Lifecycle.Disconnected();
        var reconnect = _connection.Lifecycle.Connected();
        await _callbackJoined.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _release.SetResult();
        await Task.WhenAll(_run, reconnect).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_have_published_before_the_disconnect() => _beforeDisconnect.ShouldBeFalse();
    [Fact] void should_join_the_flight_before_its_release() => _joined.ShouldBeSame(_run);
    [Fact] void should_start_another_flight_at_the_completion_boundary() => _flights.ShouldEqual(2);
    [Fact] void should_register_again_for_the_reconnected_kernel() => _services.EventStores.Received(2).EnsureEventStore(Arg.Any<EnsureEventStoreRequest>());
    [Fact] void should_clear_registering_after_the_second_flight() => _store.Registration.IsRegistering.ShouldBeFalse();
    [Fact] void should_publish_the_second_flight_as_successful() => _store.Registration.IsSuccess.ShouldBeTrue();
}
