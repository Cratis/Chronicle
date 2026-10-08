// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// In an idle namespace the unfinished-job listing is empty, and an empty listing can not tell a finished job from a
/// failed lookup. Forgetting concluded jobs only on a listing of other jobs therefore never forgot anything there, and
/// every sequential catch-up left its job behind on an observer that is kept alive. Each remembered job is confirmed
/// on its own instead, and forgotten once it has finished (Cratis/Chronicle#4548).
/// </summary>
public class when_catch_up_concludes_repeatedly_while_no_jobs_are_listed : given.an_observer_with_subscription
{
    const int LaterCompletions = 12;

    static readonly JobId _firstJob = JobId.New();

    readonly List<JobId> _concludedJobs = [];
    JobId _lastStartedJob = JobId.NotSet;

    void Establish()
    {
        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(_ =>
            {
                _lastStartedJob = JobId.New();
                return Task.FromResult(Result<JobId, StartJobError>.Success(_lastStartedJob));
            });

        _jobStorage.GetJob(Arg.Any<JobId>()).Returns(call => Task.FromResult<Catch<JobState, Cratis.Orleans.Storage.Jobs.JobError>>(new JobState
        {
            Id = call.Arg<JobId>(),
            Status = JobStatus.CompletedSuccessfully
        }));

        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)100UL);
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns((EventSequenceNumber)100UL);

        _jobStorage.ClearReceivedCalls();
    }

    async Task Because()
    {
        _concludedJobs.Add(_firstJob);
        await _observer.CaughtUp(_firstJob, 1UL);
        for (var completion = 1; completion <= LaterCompletions; completion++)
        {
            var concluding = _lastStartedJob;
            _concludedJobs.Add(concluding);
            await _observer.CaughtUp(concluding, (EventSequenceNumber)(ulong)(completion + 1));
        }
    }

    [Fact]
    async Task should_look_up_every_concluded_job_once_and_forget_it_once_it_has_finished()
    {
        foreach (var concludedJob in _concludedJobs)
        {
            await _jobStorage.Received(1).GetJob(concludedJob);
        }
    }

    [Fact] async Task should_look_up_no_other_jobs() => await _jobStorage.Received(LaterCompletions + 1).GetJob(Arg.Any<JobId>());
}
