// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// CaughtUp is AlwaysInterleave and arrives whenever the finishing job gets to send it, so a catch-up can already have
/// started a replacement job once the finishing one was finalized. The replacement has not concluded: taking every
/// catch-up job listed when CaughtUp arrives as concluded made routing start yet another job over the same events
/// and deliver them twice (Cratis/Chronicle#4548).
/// </summary>
public class when_catch_up_concludes_after_a_replacement_job_was_started : given.an_observer_with_subscription
{
    static readonly JobId _finishedJob = JobId.New();
    static readonly JobId _replacementJob = JobId.New();

    IImmutableList<JobState> _unfinishedJobs = ImmutableList<JobState>.Empty;

    async Task Establish()
    {
        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(_ => Task.FromResult(_unfinishedJobs));

        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(callInfo =>
            {
                _unfinishedJobs = ImmutableList.Create(new JobState
                {
                    Id = _replacementJob,
                    Status = JobStatus.PreparingJob,
                    Request = callInfo.Arg<CatchUpObserverRequest>()
                });
                return Task.FromResult(Result<JobId, StartJobError>.Success(_replacementJob));
            });

        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)2UL);
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns((EventSequenceNumber)2UL);

        // The finishing job has been finalized and is gone; a catch-up then starts the replacement.
        await _observer.CatchUp();
    }

    Task Because() => _observer.CaughtUp(_finishedJob, 1UL);

    [Fact]
    async Task should_only_have_started_the_replacement_job() =>
        await _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());

    [Fact] async Task should_still_be_preparing_catch_up_for_the_replacement_job() => (await _observer.IsPreparingCatchup()).ShouldBeTrue();
}
