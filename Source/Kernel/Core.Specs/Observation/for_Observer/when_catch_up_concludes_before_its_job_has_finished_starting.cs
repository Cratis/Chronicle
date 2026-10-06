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
/// A job can do its work and report back before the start that created it has returned. The catch-up that conclusion
/// routes into waits for that start and must not adopt the job it produces - it has already concluded and will never
/// report back again - but acquire a job of its own for whatever was appended meanwhile (Cratis/Chronicle#4548).
/// </summary>
public class when_catch_up_concludes_before_its_job_has_finished_starting : given.an_observer_with_subscription
{
    static readonly JobId _quickJob = JobId.New();
    static readonly JobId _followUpJob = JobId.New();
    static readonly EventSequenceNumber _lastHandledByQuickJob = 5UL;

    readonly TaskCompletionSource<Result<JobId, StartJobError>> _heldStart = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IImmutableList<JobState> _unfinishedJobs = ImmutableList<JobState>.Empty;

    void Establish()
    {
        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(_ => Task.FromResult(_unfinishedJobs));

        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(
                _ => _heldStart.Task,
                _ => Task.FromResult(Result<JobId, StartJobError>.Success(_followUpJob)));

        _eventSequence.GetTailSequenceNumber().Returns(_lastHandledByQuickJob.Next());
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(_lastHandledByQuickJob.Next());

        _jobsManager.ClearReceivedCalls();
    }

    async Task Because()
    {
        var catchUp = _observer.CatchUp();
        var caughtUp = _observer.CaughtUp(_quickJob, _lastHandledByQuickJob);

        _unfinishedJobs = ImmutableList.Create(new JobState
        {
            Id = _quickJob,
            Status = JobStatus.Running,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [])
        });
        _heldStart.SetResult(Result<JobId, StartJobError>.Success(_quickJob));

        await Task.WhenAll(catchUp, caughtUp);
    }

    [Fact]
    async Task should_start_a_follow_up_job_from_after_the_concluded_jobs_work() =>
        await _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(
            Arg.Is<CatchUpObserverRequest>(request => request.FromEventSequenceNumber == _lastHandledByQuickJob.Next()));

    [Fact]
    async Task should_start_only_the_original_and_the_follow_up_job() =>
        await _jobsManager.Received(2).Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}
