// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Storage.Jobs;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_pattern_capture;

public class and_healthy_subscription_is_still_entering_in_flight_catchup : given.an_event_sequence_with_a_capture_observer
{
    readonly TaskCompletionSource _entering = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _recoveryQueued = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task _subscription;
    ObserverRunningState _observedRunningState;

    void Establish()
    {
        _captureState.WriteStateAsync().Returns(_ =>
        {
            if (_captureState.State.RunningState == ObserverRunningState.Unknown && !_entering.Task.IsCompleted)
            {
                _entering.TrySetResult();
                return _releaseWrite.Task;
            }

            return Task.CompletedTask;
        });
        _patternCapture.Configure().RecoverSubscription(EventStore, EventStoreNamespace).Returns(async _ =>
        {
            _observedRunningState = (await _captureObserver.GetState()).RunningState;
            _recoveryQueued.TrySetResult();

            // TestKit calls methods directly. Model Orleans' non-interleaved request ordering explicitly:
            // the recovery turn waits for the Subscribe turn before checking the actual state machine.
            await _subscription;
            await RecoverCapture();
        });
    }

    async Task Because()
    {
        _subscription = SubscribeCapture();
        await _entering.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        var reconciliation = _silo.TimerRegistry.FireAllAsync();
        try
        {
            await _recoveryQueued.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
        finally
        {
            _releaseWrite.TrySetResult();
            await _subscription;
            await reconciliation;
        }
    }

    [Fact] void should_observe_the_healthy_transient_unknown_state() => _observedRunningState.ShouldEqual(ObserverRunningState.Unknown);
    [Fact] async Task should_finish_observing() => (await _captureObserver.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] async Task should_subscribe_to_the_queue_only_once() => await _appendedEventsQueues.Received(1).Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
    [Fact] async Task should_not_restart_subscription_setup() => await _jobsManager.Received(1).GetJobs(Arg.Any<JobQuery>());
}
