// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catching_up_partitions;

/// <summary>
/// The appended-events queue spilled under back-pressure: it dropped the observer's subscription and started its
/// catch-up, so nothing is delivered live until that catch-up has reported back and the observer routes again. B's
/// step reads to 10 and runs dry, B:11 is appended, and A's step goes on to 12. B:11 is delivered by no one live, and
/// the position must not move to 13 past it: it is read from where B's step got to, exactly once (#4583).
/// </summary>
public class and_the_catch_up_was_started_by_the_queue_spilling : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly Key _partitionA = "partition-a";
    static readonly Key _partitionB = "partition-b";
    static readonly JobId _catchUpJob = JobId.New();
    static readonly JobId _catchUpOfWhatWasLeftBehind = JobId.New();
    static readonly EventSequenceNumber _startedFrom = 3UL;
    static readonly EventSequenceNumber _whereTheStepOfBGotTo = 11UL;
    static readonly EventSequenceNumber _lastReadByTheStepOfA = 12UL;

    readonly List<CatchUpObserverRequest> _catchUpsStarted = [];
    EventSequenceNumber _positionWhileWhatWasLeftBehindIsRead = EventSequenceNumber.Unavailable;

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = _startedFrom };
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Do<CatchUpObserverRequest>(_catchUpsStarted.Add))
            .Returns(
                Task.FromResult(Result<JobId, StartJobError>.Success(_catchUpJob)),
                Task.FromResult(Result<JobId, StartJobError>.Success(_catchUpOfWhatWasLeftBehind)));

        // B:11 is appended after B's step concluded.
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(_whereTheStepOfBGotTo, Arg.Any<IEnumerable<EventType>>(), Arg.Is<EventSourceId>(id => id.Value == "partition-b"))
            .Returns(EventSequenceNumber.Unavailable, _whereTheStepOfBGotTo);
    }

    async Task Because()
    {
        // The queue spilling starts the catch-up, and the job's steps prepare.
        await _observer.CatchUp();
        await _observer.RegisterCatchingUpPartitions([_partitionA, _partitionB]);

        await _observer.ConcludePartitionCatchUp(_partitionB, _whereTheStepOfBGotTo, [event_type]);
        await _observer.ConcludePartitionCatchUp(_partitionA, _lastReadByTheStepOfA.Next(), [event_type]);
        await _observer.CaughtUp(_catchUpJob, _lastReadByTheStepOfA);
        _positionWhileWhatWasLeftBehindIsRead = _stateStorage.State.NextEventSequenceNumber;

        await _observer.CaughtUp(_catchUpOfWhatWasLeftBehind, _lastReadByTheStepOfA);
    }

    [Fact] void should_start_the_catch_up_and_one_for_what_it_left_behind() => _catchUpsStarted.Count.ShouldEqual(2);
    [Fact] void should_read_the_partition_left_behind_from_where_its_step_got_to() => _catchUpsStarted[1].PartitionsLeftBehind.ShouldContainOnly(new CatchUpObserverPartitionRange(_partitionB, _whereTheStepOfBGotTo));
    [Fact] void should_read_it_up_to_where_the_position_is_about_to_move() => _catchUpsStarted[1].ToEventSequenceNumber.ShouldEqual(_lastReadByTheStepOfA);
    [Fact] void should_not_move_the_position_past_it_while_it_is_read() => _positionWhileWhatWasLeftBehindIsRead.ShouldEqual(_startedFrom);
    [Fact] void should_move_the_position_on_once_it_has_been_read() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_lastReadByTheStepOfA.Next());
}
