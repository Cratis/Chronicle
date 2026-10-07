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
/// A concluded catch-up job is only forgotten once the job store confirms it has finished. A failed lookup confirms
/// nothing, so a concluded job still finalizing - and still listed as running - must not become an owner again
/// (Cratis/Chronicle#4548).
/// </summary>
public class when_catch_up_concludes_and_looking_up_the_concluded_job_fails : given.an_observer_with_subscription
{
    static readonly JobId _concludedJob = JobId.New();
    static readonly JobId _replacementJob = JobId.New();
    static readonly EventSequenceNumber _lastHandledByConcludedJob = 5UL;

    void Establish()
    {
        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(new JobState
            {
                Id = _concludedJob,
                Status = JobStatus.Running,
                Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [])
            })));

        _jobStorage
            .GetJob(_concludedJob)
            .Returns(Task.FromResult<Catch<JobState, Cratis.Orleans.Storage.Jobs.JobError>>(new JobStorageUnavailable()));

        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_replacementJob)));

        // An event was appended at the boundary, so the conclusion asks for a replacement.
        _eventSequence.GetTailSequenceNumber().Returns(_lastHandledByConcludedJob.Next());
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(_lastHandledByConcludedJob.Next());

        _jobsManager.ClearReceivedCalls();
        _jobStorage.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.CaughtUp(_concludedJob, _lastHandledByConcludedJob);
        await _observer.CatchUp();
    }

    [Fact] async Task should_have_looked_up_the_concluded_job() => await _jobStorage.Received().GetJob(_concludedJob);

    [Fact]
    async Task should_start_a_replacement_every_time_instead_of_adopting_the_concluded_job() =>
        await _jobsManager.Received(2).Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());

    class JobStorageUnavailable() : Exception("The job storage is unavailable");
}
