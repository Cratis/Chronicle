// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_pattern_capture;

public class and_an_ordinary_retry_reloads_active_after_failed_resubscription : given.an_event_sequence_with_snapshotting_capture_storage
{
    readonly FailedPartition _quarantinedPartition = new() { Partition = (Key)"quarantined-partition", IsQuarantined = true };
    Exception _resubscriptionError;
    Exception _ordinaryRetryError;
    Type _stateAfterOrdinaryRetry;
    ObserverRunningState _persistedAfterFailure;
    ObserverRunningState _runningStateAfterOrdinaryRetry;
    EventSequenceNumber _nextAfterRecovery;
    bool _queueSubscribed;
    bool _queueSubscribedAfterOrdinaryRetry;

    async Task Establish()
    {
        _captureFailures.Partitions = [_quarantinedPartition];
        _appendedEventsQueues.Configure().Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>())
            .Returns(call =>
            {
                _queueSubscribed = true;
                return new AppendedEventsQueueSubscription(call.Arg<ObserverKey>(), 0);
            });
        _appendedEventsQueues.Unsubscribe(Arg.Any<AppendedEventsQueueSubscription>()).Returns(_ =>
        {
            _queueSubscribed = false;
            return Task.CompletedTask;
        });

        await SubscribeCapture();
        (await _captureObserver.GetCurrentState()).ShouldBeOfExactType<Observing>();
        _persistedCaptureState.RunningState.ShouldEqual(ObserverRunningState.Active);
        _queueSubscribed.ShouldBeTrue();

        // Re-subscription first persists Active, then leaves Observing and loses the scheduled Routing
        // transition when CatchingUpInFlight's entry write fails. A real read restores the Active snapshot.
        _failNextCaptureEntryWrite = true;
        _resubscriptionError = await Catch.Exception(SubscribeCapture);
        _persistedAfterFailure = _persistedCaptureState.RunningState;
        _ordinaryRetryError = await Catch.Exception(SubscribeCapture);
        _stateAfterOrdinaryRetry = (await _captureObserver.GetCurrentState()).GetType();
        _runningStateAfterOrdinaryRetry = (await _captureObserver.GetState()).RunningState;
        _queueSubscribedAfterOrdinaryRetry = _queueSubscribed;
        _captureState.ClearReceivedCalls();
        _appendedEventsQueues.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _nextAfterRecovery = (await _captureObserver.GetState()).NextEventSequenceNumber;
        if (_queueSubscribed)
        {
            await _captureObserver.Handle("healthy-partition", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_eventType, 43UL)]);
        }
    }

    [Fact] void should_have_failed_the_resubscription_entry_write() => _resubscriptionError.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_have_kept_active_in_storage_after_the_failure() => _persistedAfterFailure.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_demonstrate_the_ordinary_retry_returns_successfully() => _ordinaryRetryError.ShouldBeNull();
    [Fact] void should_have_reloaded_the_active_marker() => _runningStateAfterOrdinaryRetry.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_still_be_stalled_after_the_ordinary_retry() => _stateAfterOrdinaryRetry.ShouldEqual(typeof(CatchingUpInFlight));
    [Fact] void should_still_be_off_the_queue_after_the_ordinary_retry() => _queueSubscribedAfterOrdinaryRetry.ShouldBeFalse();
    [Fact] async Task should_resume_observing() => (await _captureObserver.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] async Task should_restore_queue_delivery_once() => await _appendedEventsQueues.Received(1).Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
    [Fact] void should_preserve_the_next_sequence_number() => _nextAfterRecovery.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] async Task should_not_reload_state_during_recovery() => await _captureState.DidNotReceive().ReadStateAsync();
    [Fact] async Task should_deliver_the_next_healthy_event() => await _captureSubscriber.Received(1).OnNext(Arg.Any<Key>(), Arg.Is<IEnumerable<AppendedEvent>>(events => events.Single().Context.SequenceNumber == 43UL), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_persist_the_handled_event() => _persistedCaptureState.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_retain_the_failed_partition() => _captureFailures.Partitions.ShouldContainOnly(_quarantinedPartition);
    [Fact] void should_keep_the_partition_quarantined() => _quarantinedPartition.IsQuarantined.ShouldBeTrue();
    [Fact] async Task should_not_retry_the_quarantined_partition() => await _jobsManager.DidNotReceive().Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
}
