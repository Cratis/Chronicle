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
/// A second catch-up arrives while the first is still acquiring its job, after the job's steps already prepared, and
/// adopts that job - raising the preparing flag again with nothing left to lower it until the job reports back. Live
/// delivery drops every event meanwhile. B's step reads to 10 and runs dry, B:11 is appended and dropped live, and A's
/// step goes on to 12. B:11 must still be read, from where B's step got to, before the position moves past it (#4583).
/// </summary>
public class and_an_adopted_catch_up_left_preparation_raised : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly Key _partitionA = "partition-a";
    static readonly Key _partitionB = "partition-b";
    static readonly JobId _catchUpJob = JobId.New();
    static readonly JobId _catchUpOfWhatWasLeftBehind = JobId.New();
    static readonly EventSequenceNumber _startedFrom = 3UL;
    static readonly EventSequenceNumber _whereTheStepOfBGotTo = 11UL;
    static readonly EventSequenceNumber _lastReadByTheStepOfA = 12UL;

    readonly TaskCompletionSource<Result<JobId, StartJobError>> _catchUpJobStart = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly List<CatchUpObserverRequest> _catchUpsStarted = [];
    readonly List<EventSequenceNumber> _deliveredLive = [];
    bool _preparingWhenTheEventArrivedLive;
    EventSequenceNumber _positionWhileWhatWasLeftBehindIsRead = EventSequenceNumber.Unavailable;

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = _startedFrom };
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Do<CatchUpObserverRequest>(_catchUpsStarted.Add))
            .Returns(
                _ => _catchUpJobStart.Task,
                _ => Task.FromResult(Result<JobId, StartJobError>.Success(_catchUpOfWhatWasLeftBehind)));

        // B:11 is appended after B's step concluded.
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(_whereTheStepOfBGotTo, Arg.Any<IEnumerable<EventType>>(), Arg.Is<EventSourceId>(id => id.Value == "partition-b"))
            .Returns(EventSequenceNumber.Unavailable, _whereTheStepOfBGotTo);
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
        var acquiring = _observer.CatchUp();
        await _observer.RegisterCatchingUpPartitions([_partitionA, _partitionB]);
        var adopting = _observer.CatchUp();
        _catchUpJobStart.SetResult(Result<JobId, StartJobError>.Success(_catchUpJob));
        await Task.WhenAll(acquiring, adopting);

        await _observer.ConcludePartitionCatchUp(_partitionB, _whereTheStepOfBGotTo, [event_type]);
        _preparingWhenTheEventArrivedLive = await _observer.IsPreparingCatchup();
        await _observer.Handle(_partitionB, [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, _whereTheStepOfBGotTo)]);
        await _observer.ConcludePartitionCatchUp(_partitionA, _lastReadByTheStepOfA.Next(), [event_type]);
        await _observer.CaughtUp(_catchUpJob, _lastReadByTheStepOfA);
        _positionWhileWhatWasLeftBehindIsRead = _stateStorage.State.NextEventSequenceNumber;
    }

    [Fact] void should_have_been_preparing_catch_up_when_the_event_arrived_live() => _preparingWhenTheEventArrivedLive.ShouldBeTrue();
    [Fact] void should_not_deliver_the_event_live() => _deliveredLive.ShouldBeEmpty();
    [Fact] void should_start_the_catch_up_once_and_one_for_what_it_left_behind() => _catchUpsStarted.Count.ShouldEqual(2);
    [Fact] void should_read_the_partition_left_behind_from_where_its_step_got_to() => _catchUpsStarted[1].PartitionsLeftBehind.ShouldContainOnly(new CatchUpObserverPartitionRange(_partitionB, _whereTheStepOfBGotTo));
    [Fact] void should_not_move_the_position_past_it_while_it_is_read() => _positionWhileWhatWasLeftBehindIsRead.ShouldEqual(_startedFrom);
}
