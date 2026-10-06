// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// The whole round trip of an event appended between a catch-up job reporting back and that job being finalized: the
/// finishing job, still listed as running, must not be taken as the owner; a new catch-up starts from the event; and
/// once that catch-up has prepared its partitions and reported back, the observer has moved past the event and is no
/// longer preparing catch-up - so it is not left dropping live events and skipping its missed-events check (#4548).
/// </summary>
public class when_an_event_is_appended_while_catch_up_is_finishing : given.an_observer_with_subscription
{
    static readonly JobId _finishingJob = JobId.New();
    static readonly JobId _replacementJob = JobId.New();
    static readonly EventSequenceNumber _lastHandledByFinishingJob = 1UL;
    static readonly EventSequenceNumber _eventAppendedAtTheBoundary = 2UL;
    static readonly Key _partitionOfTheAppendedEvent = "new-partition";

    bool _wasPreparingCatchupBeforeTheReplacementPrepared;
    bool _isPreparingCatchupAfterwards;

    void Establish()
    {
        var finishing = new JobState
        {
            Id = _finishingJob,
            Status = JobStatus.Running,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [])
        };
        var replacement = new JobState
        {
            Id = _replacementJob,
            Status = JobStatus.Running,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, _eventAppendedAtTheBoundary, [])
        };
        var replacementStarted = false;

        // Once the replacement has started it is listed as well, while the finishing job is still being finalized.
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(_ => Task.FromResult<IImmutableList<JobState>>(
            replacementStarted ? [finishing, replacement] : [finishing]));
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(_ =>
            {
                replacementStarted = true;
                return Task.FromResult(Result<JobId, StartJobError>.Success(_replacementJob));
            });

        _eventSequence.GetTailSequenceNumber().Returns(_eventAppendedAtTheBoundary);
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(call => call.Arg<EventSequenceNumber>() <= _eventAppendedAtTheBoundary ? _eventAppendedAtTheBoundary : EventSequenceNumber.Unavailable);
    }

    async Task Because()
    {
        await _observer.CaughtUp(_finishingJob, _lastHandledByFinishingJob);
        _wasPreparingCatchupBeforeTheReplacementPrepared = await _observer.IsPreparingCatchup();

        // What the replacement job does: register the partition it catches up, handle the event and report back.
        await _observer.RegisterCatchingUpPartitions([_partitionOfTheAppendedEvent]);
        await _observer.CaughtUp(_replacementJob, _eventAppendedAtTheBoundary);
        _isPreparingCatchupAfterwards = await _observer.IsPreparingCatchup();
    }

    [Fact]
    void should_start_exactly_one_catch_up_job_from_the_appended_event() =>
        _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(
            Arg.Is<CatchUpObserverRequest>(request => request.FromEventSequenceNumber == _eventAppendedAtTheBoundary));

    [Fact] void should_be_preparing_catch_up_until_the_replacement_prepares() => _wasPreparingCatchupBeforeTheReplacementPrepared.ShouldBeTrue();
    [Fact] void should_not_be_left_preparing_catch_up() => _isPreparingCatchupAfterwards.ShouldBeFalse();
    [Fact] void should_have_handled_the_appended_event() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(_eventAppendedAtTheBoundary);
    [Fact] void should_move_past_the_appended_event() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_eventAppendedAtTheBoundary.Next());
    [Fact] void should_not_hold_back_live_events_for_any_partition() => _stateStorage.State.CatchingUpPartitions.ShouldBeEmpty();
}
