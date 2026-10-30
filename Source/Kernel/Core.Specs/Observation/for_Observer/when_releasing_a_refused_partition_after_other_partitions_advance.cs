// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_releasing_a_refused_partition_after_other_partitions_advance : given.an_observer_with_subscription_for_specific_event_type
{
    ReplayObserverPartitionRequest _request;
    EventSequenceNumber _nextBeforeRelease;
    bool _markedBeforeAdmission;
    bool _persistedBeforeAdmission;

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { LastHandledEventSequenceNumber = 44UL, NextEventSequenceNumber = 45UL };
        _ = _jobsManager.Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(Arg.Do<ReplayObserverPartitionRequest>(request =>
        {
            _request = request;
            _markedBeforeAdmission = _stateStorage.State.ReplayingPartitions.Contains("A");
            _persistedBeforeAdmission = _storageStats.Writes > 0;
        }));
        _subscriber.OnNext((Key)"B", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(ObserverSubscriberResult.Ok(50UL));
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(45UL, Arg.Any<IEnumerable<EventType>>(), (EventSourceId)"A").Returns((EventSequenceNumber)45UL);
    }

    async Task Because()
    {
        await _observer.ReplayPartition("A");
        await _observer.Handle("A", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 45UL)]);
        await _observer.Handle("B", Enumerable.Range(46, 5).Select(number => AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, (ulong)number)));
        _nextBeforeRelease = _stateStorage.State.NextEventSequenceNumber;
        await _observer.ReleaseRefusedPartitionReplay("A", _request.HeldBackFrom);
    }

    [Fact] void should_mark_before_starting_the_job() => _markedBeforeAdmission.ShouldBeTrue();
    [Fact] void should_persist_before_starting_the_job() => _persistedBeforeAdmission.ShouldBeTrue();
    [Fact] void should_persist_the_held_back_lower_bound_in_the_request() => _request.HeldBackFrom.ShouldEqual((EventSequenceNumber)45UL);
    [Fact] void should_have_advanced_shared_progress_through_the_other_partition() => _nextBeforeRelease.ShouldEqual((EventSequenceNumber)51UL);
    [Fact] void should_preserve_handled_progress() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)50UL);
    [Fact] void should_preserve_next_progress() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)51UL);
    [Fact] void should_not_deliver_held_back_events_live() => _subscriber.DidNotReceive().OnNext((Key)"A", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_catch_up_starting_with_the_held_back_event() => _jobsManager.Received(1)
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(_ => _.Key == (Key)"A" && _.FromSequenceNumber == (EventSequenceNumber)45UL));
}
