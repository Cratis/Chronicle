// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_pattern_capture;

public class and_entering_in_flight_catchup_could_not_be_persisted : given.an_event_sequence_with_a_capture_observer
{
    Exception _initialError;
    Exception _ordinaryRetryError;
    Type _stalledState;
    ObserverRunningState _runningStateAfterOrdinaryRetry;
    EventSequenceNumber _nextAfterRecovery;
    bool _failedWrite;

    async Task Establish()
    {
        _captureState.WriteStateAsync().Returns(_ =>
        {
            if (!_failedWrite && _captureState.State.RunningState == ObserverRunningState.Unknown)
            {
                _failedWrite = true;
                return Task.FromException(new TimeoutException());
            }

            return Task.CompletedTask;
        });
        _initialError = await Catch.Exception(SubscribeCapture);
        _stalledState = (await _captureObserver.GetCurrentState()).GetType();

        // The old reconciliation used this same ordinary Subscribe: it can return without restarting routing.
        _ordinaryRetryError = await Catch.Exception(SubscribeCapture);
        _runningStateAfterOrdinaryRetry = (await _captureObserver.GetState()).RunningState;
        _captureState.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _nextAfterRecovery = (await _captureObserver.GetState()).NextEventSequenceNumber;
        await _captureObserver.Handle("next-partition", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_eventType, 43UL)]);
    }

    [Fact] void should_have_failed_the_entry_write() => _initialError.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_have_left_the_state_machine_in_in_flight_catchup() => _stalledState.ShouldEqual(typeof(CatchingUpInFlight));
    [Fact] void should_demonstrate_the_ordinary_retry_returns_successfully() => _ordinaryRetryError.ShouldBeNull();
    [Fact] void should_demonstrate_the_ordinary_retry_does_not_restore_observation() => _runningStateAfterOrdinaryRetry.ShouldEqual(ObserverRunningState.Unknown);
    [Fact] async Task should_resume_observing() => (await _captureObserver.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_preserve_the_next_sequence_number() => _nextAfterRecovery.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] async Task should_not_reload_stale_progress() => await _captureState.DidNotReceive().ReadStateAsync();
    [Fact] async Task should_deliver_the_next_event() => await _captureSubscriber.Received(1).OnNext(Arg.Any<Key>(), Arg.Is<IEnumerable<AppendedEvent>>(events => events.Single().Context.SequenceNumber == 43UL), Arg.Any<ObserverSubscriberContext>());
    [Fact] async Task should_record_the_handled_event() => (await _captureObserver.GetState()).LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
}
