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
/// A catch-up looking up unfinished jobs excludes a job that concluded while the lookup was in flight, and starts a
/// replacement. Building that replacement's request before the lookup gave it the position from before the concluded
/// job's work, so it delivered that work a second time - and the catch-up the conclusion routed into adopted it
/// (Cratis/Chronicle#4548).
/// </summary>
public class when_catch_up_concludes_while_a_catch_up_is_looking_up_jobs : given.an_observer_with_subscription
{
    static readonly JobId _concludingJob = JobId.New();
    static readonly JobId _replacementJob = JobId.New();
    static readonly EventSequenceNumber _lastHandledByConcludingJob = 5UL;

    readonly TaskCompletionSource<IImmutableList<JobState>> _heldLookup = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IImmutableList<JobState> _unfinishedJobs = ImmutableList<JobState>.Empty;
    bool _holdNextLookup;

    void Establish()
    {
        _unfinishedJobs = ImmutableList.Create(new JobState
        {
            Id = _concludingJob,
            Status = JobStatus.Running,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [])
        });

        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(_ =>
            {
                if (!_holdNextLookup) return Task.FromResult(_unfinishedJobs);
                _holdNextLookup = false;
                return _heldLookup.Task;
            });

        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_replacementJob)));

        _eventSequence.GetTailSequenceNumber().Returns(_lastHandledByConcludingJob.Next());
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(_lastHandledByConcludingJob.Next());

        _jobsManager.ClearReceivedCalls();
    }

    async Task Because()
    {
        _holdNextLookup = true;
        var catchUp = _observer.CatchUp();
        var caughtUp = _observer.CaughtUp(_concludingJob, _lastHandledByConcludingJob);
        _heldLookup.SetResult(_unfinishedJobs);
        await Task.WhenAll(catchUp, caughtUp);
    }

    [Fact]
    async Task should_start_exactly_one_replacement_job_from_after_the_concluded_jobs_work() =>
        await _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(
            Arg.Is<CatchUpObserverRequest>(request => request.FromEventSequenceNumber == _lastHandledByConcludingJob.Next()));

    [Fact]
    async Task should_not_start_any_other_job() =>
        await _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}
