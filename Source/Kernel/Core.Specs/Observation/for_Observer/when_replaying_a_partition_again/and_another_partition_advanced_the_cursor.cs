// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_replaying_a_partition_again;

public class and_another_partition_advanced_the_cursor : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly Key _partition = "A";
    static readonly Key _otherPartition = "B";
    static readonly EventType _selectedType = new("selected-type", EventTypeGeneration.First);
    static readonly EventSequenceNumber _lastRead = 44UL;
    readonly List<ReplayObserverPartitionRequest> _starts = [];
    EventSequenceNumber _nextBeforeCompletion;
    ReplayObserverPartitionRequest _followUp;

    void Establish()
    {
        _observer.SetSubscription(subscription with { EventTypes = [event_type, _selectedType] });
        _stateStorage.State = _stateStorage.State with { LastHandledEventSequenceNumber = _lastRead, NextEventSequenceNumber = _lastRead.Next() };
        _ = _jobsManager.Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(Arg.Any<JobId>(), Arg.Do<ReplayObserverPartitionRequest>(_starts.Add));
        _subscriber.OnNext(_otherPartition, Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(ObserverSubscriberResult.Ok(50UL));
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(_lastRead.Next(), Arg.Any<IEnumerable<EventType>>(), _partition).Returns(_lastRead.Next());
    }

    async Task Because()
    {
        await _observer.ReplayPartitionTo(_partition, _lastRead);
        var first = _starts.Single();
        await _observer.ReplayPartition(_partition, [_selectedType]);
        await _observer.Handle(_partition, [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, _lastRead.Next())]);
        await _observer.Handle(_otherPartition, Enumerable.Range(46, 5).Select(number => AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, (ulong)number)));
        _nextBeforeCompletion = _stateStorage.State.NextEventSequenceNumber;

        await _observer.PartitionReplayed(_partition, _lastRead, _lastRead, [], first.OwnershipEpoch);
        _followUp = _starts[1];

        // The selected-type job reads into the live-delivery hold, so it reports coverage only below that hold.
        await _observer.PartitionReplayed(_partition, EventSequenceNumber.Unavailable, _followUp.HeldBackFrom - 1UL, [], _followUp.OwnershipEpoch);
    }

    [Fact] void should_have_advanced_the_shared_cursor() => _nextBeforeCompletion.ShouldEqual((EventSequenceNumber)51UL);
    [Fact] void should_start_the_selected_type_follow_up() => _followUp.EventTypes.ShouldEqual(new[] { _selectedType });
    [Fact] void should_preserve_the_original_held_back_position() => _followUp.HeldBackFrom.ShouldEqual(_lastRead.Next());
    [Fact] void should_not_deliver_the_held_back_event_live() => _subscriber.DidNotReceive().OnNext(_partition, Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_catch_up_the_event_the_selected_replay_did_not_read() => _jobsManager.Received(1)
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(_ => _.Key == _partition && _.FromSequenceNumber == _lastRead.Next()));
}
