// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catching_up_partitions;

/// <summary>
/// Events are stored before the appended-events queue delivers them, so partition B's step can read event 100 from
/// storage and run dry while the queue still has 100 on its way. Partition A is still catching up, so the observer's
/// position is still where the catch-up started. Live delivery of 100 - alone, and in a batch with 101 and 102 that
/// were appended after the step ran dry - must not reach the subscriber: 100 was delivered by the step, and 101 and
/// 102 are read from where the step got to once the catch-up reports back (#4583).
/// </summary>
public class and_live_delivery_brings_events_its_step_already_read : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly Key _partitionA = "partition-a";
    static readonly Key _partitionB = "partition-b";
    static readonly JobId _catchUpJob = JobId.New();
    static readonly JobId _catchUpOfWhatWasLeftBehind = JobId.New();
    static readonly EventSequenceNumber _readByTheStep = 100UL;
    static readonly EventSequenceNumber _whereTheStepGotTo = 101UL;
    static readonly EventSequenceNumber _appendedAfterTheStepRanDry = 102UL;

    readonly List<EventSequenceNumber> _deliveredLive = [];
    readonly List<CatchUpObserverRequest> _catchUpsStarted = [];
    bool _concluded;

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 5UL };
        _stateStorage.State.CatchingUpPartitions.Add(_partitionA);
        _stateStorage.State.CatchingUpPartitions.Add(_partitionB);

        // Nothing is left unread when the step concludes; 101 and 102 are appended afterwards.
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(_whereTheStepGotTo, Arg.Any<IEnumerable<EventType>>(), Arg.Is<EventSourceId>(id => id.Value == "partition-b"))
            .Returns(EventSequenceNumber.Unavailable, _whereTheStepGotTo);
        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Do<CatchUpObserverRequest>(_catchUpsStarted.Add))
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_catchUpOfWhatWasLeftBehind)));
        _subscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call =>
            {
                var events = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
                _deliveredLive.AddRange(events.Select(_ => _.Context.SequenceNumber));
                return Task.FromResult(ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber));
            });
    }

    async Task Because()
    {
        _concluded = await _observer.ConcludePartitionCatchUp(_partitionB, _whereTheStepGotTo, [event_type]);
        await _observer.Handle(_partitionB, [Event(_readByTheStep)]);
        await _observer.Handle(_partitionB, [Event(_readByTheStep), Event(_whereTheStepGotTo), Event(_appendedAfterTheStepRanDry)]);
        await _observer.CaughtUp(_catchUpJob, _appendedAfterTheStepRanDry);
    }

    [Fact] void should_conclude_the_step() => _concluded.ShouldBeTrue();
    [Fact] void should_not_deliver_anything_live_while_the_partition_is_catching_up() => _deliveredLive.ShouldBeEmpty();
    [Fact] void should_start_one_catch_up_for_what_the_step_left_behind() => _catchUpsStarted.Count.ShouldEqual(1);
    [Fact] void should_read_the_partition_from_where_its_step_got_to() => _catchUpsStarted[0].PartitionsLeftBehind.ShouldContainOnly(new CatchUpObserverPartitionRange(_partitionB, _whereTheStepGotTo));
    [Fact] void should_read_it_up_to_where_the_position_is_about_to_move() => _catchUpsStarted[0].ToEventSequenceNumber.ShouldEqual(_appendedAfterTheStepRanDry);
    [Fact] void should_not_move_the_position_past_what_was_left_behind() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)5UL);

    static AppendedEvent Event(EventSequenceNumber sequenceNumber) => AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, sequenceNumber);
}
