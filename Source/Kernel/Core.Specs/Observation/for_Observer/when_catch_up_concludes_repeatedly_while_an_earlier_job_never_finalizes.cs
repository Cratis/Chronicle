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
/// A concluded catch-up job whose finalization is slow or failed stays listed as unfinished. Forgetting it after a fixed
/// number of later completions made it adoptable again, and catch-up was then handed to a job that will never report
/// back (Cratis/Chronicle#4548).
/// </summary>
public class when_catch_up_concludes_repeatedly_while_an_earlier_job_never_finalizes : given.an_observer_with_subscription
{
    const int LaterCompletions = 12;

    static readonly JobId _neverFinalizingJob = JobId.New();

    JobId _lastStartedJob = JobId.NotSet;
    IImmutableList<JobState> _unfinishedJobs = ImmutableList<JobState>.Empty;

    void Establish()
    {
        _unfinishedJobs = ImmutableList.Create(CatchUpJob(_neverFinalizingJob));

        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(_ => Task.FromResult(_unfinishedJobs));

        // Every started job is listed until the next one starts - it finalizes while its successor gets going - while
        // the never-finalizing job stays listed throughout.
        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(_ =>
            {
                _lastStartedJob = JobId.New();
                _unfinishedJobs = ImmutableList.Create(CatchUpJob(_neverFinalizingJob), CatchUpJob(_lastStartedJob));
                return Task.FromResult(Result<JobId, StartJobError>.Success(_lastStartedJob));
            });

        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)100UL);
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns((EventSequenceNumber)100UL);
    }

    async Task Because()
    {
        await _observer.CaughtUp(_neverFinalizingJob, 1UL);
        for (var completion = 1; completion <= LaterCompletions; completion++)
        {
            await _observer.CaughtUp(_lastStartedJob, (EventSequenceNumber)(ulong)(completion + 1));
        }
    }

    [Fact]
    async Task should_start_a_new_job_after_every_completion_instead_of_adopting_the_concluded_one() =>
        await _jobsManager.Received(LaterCompletions + 1).Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());

    JobState CatchUpJob(JobId jobId) => new()
    {
        Id = jobId,
        Status = JobStatus.Running,
        Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [])
    };
}
