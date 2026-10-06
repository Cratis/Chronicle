// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A job being started is not listed until its start completes. CaughtUp is AlwaysInterleave, so routing it into a
/// catch-up can overlap with another catch-up - such as one the appended-events queue triggers - and each of them,
/// finding only the concluded job, started its own replacement over the same events (Cratis/Chronicle#4548).
/// </summary>
public class when_catch_up_concludes_while_a_replacement_job_is_still_starting : given.an_observer_with_subscription
{
    static readonly JobId _finishingJob = JobId.New();
    static readonly JobId _replacementJob = JobId.New();

    readonly TaskCompletionSource<Result<JobId, StartJobError>> _replacementStart = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IImmutableList<JobState> _unfinishedJobs = ImmutableList<JobState>.Empty;

    void Establish()
    {
        _unfinishedJobs = ImmutableList.Create(new JobState
        {
            Id = _finishingJob,
            Status = JobStatus.Running,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [])
        });

        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(_ => Task.FromResult(_unfinishedJobs));

        // The first replacement's start is held, so it is not yet listed while the other catch-up arrives.
        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(
                _ => _replacementStart.Task,
                _ => Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));

        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)2UL);
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns((EventSequenceNumber)2UL);
    }

    async Task Because()
    {
        var caughtUp = _observer.CaughtUp(_finishingJob, 1UL);
        var catchUp = _observer.CatchUp();

        _unfinishedJobs = _unfinishedJobs.Add(new JobState
        {
            Id = _replacementJob,
            Status = JobStatus.PreparingJob,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, (EventSequenceNumber)2UL, [])
        });
        _replacementStart.SetResult(Result<JobId, StartJobError>.Success(_replacementJob));

        await Task.WhenAll(caughtUp, catchUp);
    }

    [Fact]
    async Task should_start_exactly_one_replacement_job() =>
        await _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());

    [Fact] async Task should_still_be_preparing_catch_up_for_the_replacement_job() => (await _observer.IsPreparingCatchup()).ShouldBeTrue();
}
