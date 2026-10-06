// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catching_up_partitions;

/// <summary>
/// Partition B's step read up to 99 and ran dry. Event 100 for B is then appended, but the queue has not delivered it
/// when partition D's step reads 101 and the catch-up reports back. Catch-up reporting back interleaves with live
/// delivery, so the observer's position would move to 102 before the live delivery of 100 arrives - and filter it out,
/// leaving it to nobody. Instead 100 is read from where B's step got to before the position moves on, and the live
/// delivery that arrives meanwhile does not deliver it a second time: the subscriber gets 100 exactly once (#4583).
/// </summary>
public class and_an_event_for_a_partition_is_still_on_its_way_when_the_catch_up_reports_back : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly Key _partitionB = "partition-b";
    static readonly Key _partitionD = "partition-d";
    static readonly JobId _catchUpJob = JobId.New();
    static readonly JobId _catchUpOfWhatWasLeftBehind = JobId.New();
    static readonly EventSequenceNumber _whereTheStepOfBGotTo = 100UL;
    static readonly EventSequenceNumber _readByTheStepOfD = 101UL;

    readonly List<EventSequenceNumber> _deliveredLive = [];
    readonly List<CatchUpObserverRequest> _catchUpsStarted = [];
    EventSequenceNumber _positionWhileWhatWasLeftBehindIsRead = EventSequenceNumber.Unavailable;

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 5UL };
        _stateStorage.State.CatchingUpPartitions.Add(_partitionB);
        _stateStorage.State.CatchingUpPartitions.Add(_partitionD);

        // Event 100 for B is appended after B's step concluded.
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(_whereTheStepOfBGotTo, Arg.Any<IEnumerable<EventType>>(), Arg.Is<EventSourceId>(id => id.Value == "partition-b"))
            .Returns(EventSequenceNumber.Unavailable, _whereTheStepOfBGotTo);
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
        await _observer.ConcludePartitionCatchUp(_partitionB, _whereTheStepOfBGotTo, [event_type]);
        await _observer.ConcludePartitionCatchUp(_partitionD, _readByTheStepOfD.Next(), [event_type]);
        await _observer.CaughtUp(_catchUpJob, _readByTheStepOfD);

        // The live delivery of 100 arrives while what was left behind is being read.
        await _observer.Handle(_partitionB, [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, _whereTheStepOfBGotTo)]);
        _positionWhileWhatWasLeftBehindIsRead = _stateStorage.State.NextEventSequenceNumber;

        // Its step has read B from 100 up to 101 and reports back.
        await _observer.CaughtUp(_catchUpOfWhatWasLeftBehind, _readByTheStepOfD);
    }

    [Fact] void should_not_deliver_the_event_left_behind_live() => _deliveredLive.ShouldBeEmpty();
    [Fact] void should_start_one_catch_up_for_what_was_left_behind() => _catchUpsStarted.Count.ShouldEqual(1);
    [Fact] void should_read_only_the_partition_left_behind_from_where_its_step_got_to() => _catchUpsStarted[0].PartitionsLeftBehind.ShouldContainOnly(new CatchUpObserverPartitionRange(_partitionB, _whereTheStepOfBGotTo));
    [Fact] void should_read_it_up_to_where_the_position_is_about_to_move() => _catchUpsStarted[0].ToEventSequenceNumber.ShouldEqual(_readByTheStepOfD);
    [Fact] void should_not_move_the_position_past_it_while_it_is_read() => _positionWhileWhatWasLeftBehindIsRead.ShouldEqual((EventSequenceNumber)5UL);
    [Fact] void should_move_the_position_past_both_once_what_was_left_behind_has_been_read() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_readByTheStepOfD.Next());
    [Fact] void should_release_the_partitions_once_what_was_left_behind_has_been_read() => _stateStorage.State.CatchingUpPartitions.ShouldBeEmpty();
}
