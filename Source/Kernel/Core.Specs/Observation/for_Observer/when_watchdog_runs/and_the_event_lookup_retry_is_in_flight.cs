// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.States;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_the_event_lookup_retry_is_in_flight : given.an_observer_waiting_for_event_lookup_retry
{
    readonly TaskCompletionSource _lookupStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<Result<EventSequenceNumber, GetSequenceNumberError>> _lookupResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task _routing;
    int _lookupsWhileInFlight;

    async Task Establish()
    {
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(_ =>
            {
                _lookupStarted.TrySetResult();
                return _lookupResult.Task;
            });
        _routing = _observer.TransitionTo<Routing>();
        await _lookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        try
        {
            await _observer.RunWatchdogAsync();
            _lookupsWhileInFlight = LookupCount();
        }
        finally
        {
            _lookupResult.TrySetResult(GetSequenceNumberError.StorageError);
            await _routing.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
    }

    [Fact] void should_not_retry_an_in_flight_lookup() => _lookupsWhileInFlight.ShouldEqual(1);
    [Fact] void should_not_schedule_another_routing_pass() => LookupCount().ShouldEqual(1);
    [Fact] void should_not_start_catch_up() => ShouldNotHaveStartedCatchup();
}
