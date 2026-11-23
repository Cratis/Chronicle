// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Properties;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// Two reports can await their partition-hold lookups together. Completing the newer one first, even if its routing
/// fails, must neither release the older one's barrier nor let the older report move the cursor backwards (#6145).
/// </summary>
public class when_overlapping_catch_up_conclusions_finish_out_of_order : given.an_observer_with_subscription
{
    readonly TaskCompletionSource<Catch<IImmutableList<JobState>>> _olderLookup = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<Catch<IImmutableList<JobState>>> _newerLookup = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly JobId _replayJob = JobId.New();
    Exception? _newerError;
    bool _catchUpWaitedForOlderConclusion;
    EventSequenceNumber _positionAfterBothConclusions;
    JobId _replayResult = JobId.NotSet;

    async Task Establish()
    {
        await _observer.RegisterCatchingUpPartitions([new Key("A", ArrayIndexers.NoIndexers)]);
        _jobStorage.GetJobs(Arg.Any<JobQuery>()).Returns(
            _ => _olderLookup.Task,
            _ => _newerLookup.Task,
            _ => Task.FromResult(Catch<IImmutableList<JobState>>.Success(ImmutableList<JobState>.Empty)));
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
        _jobsManager.Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_replayJob)));
        _eventSequence.GetTailSequenceNumber().Returns(
            _ => Task.FromException<EventSequenceNumber>(new InvalidOperationException("Reading the tail failed")),
            _ => Task.FromResult((EventSequenceNumber)12));
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns((EventSequenceNumber)9);
    }

    async Task Because()
    {
        var older = _observer.CaughtUp(JobId.New(), 4);
        var newer = _observer.CaughtUp(JobId.New(), 8);
        _newerLookup.SetResult(Catch<IImmutableList<JobState>>.Success(ImmutableList<JobState>.Empty));
        _newerError = await Cratis.Specifications.Catch.Exception(() => newer.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));

        var catchUp = _observer.CatchUp();
        _catchUpWaitedForOlderConclusion = !catchUp.IsCompleted;
        _olderLookup.SetResult(Catch<IImmutableList<JobState>>.Success(ImmutableList<JobState>.Empty));
        await Task.WhenAll(older, catchUp).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _positionAfterBothConclusions = _stateStorage.State.NextEventSequenceNumber;
        _replayResult = await _observer.Replay().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_observe_the_failed_routing() => _newerError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_keep_waiting_for_the_older_conclusion() => _catchUpWaitedForOlderConclusion.ShouldBeTrue();
    [Fact] void should_keep_the_furthest_reported_position() => _positionAfterBothConclusions.ShouldEqual((EventSequenceNumber)9);
    [Fact] void should_release_all_handover_counters_for_replay() => _replayResult.ShouldEqual(_replayJob);
    [Fact] void should_be_replaying() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Replaying);

    [Fact]
    async Task should_not_start_from_the_older_reports_position() => await _jobsManager.DidNotReceive()
        .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Is<CatchUpObserverRequest>(request => request.FromEventSequenceNumber < (EventSequenceNumber)9));
}
