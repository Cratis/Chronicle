// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Observation.for_Observer.when_stopping_catch_up_ownership;

/// <summary>
/// No generation is issued while a fast-forward is stopping and deleting the skipped jobs, and the deleted job cannot register afterwards (#5406).
/// </summary>
public class and_a_resume_interleaves_with_a_fast_forward : given.an_observer_with_a_gated_state_write
{
    static readonly JobId _jobId = JobId.New();
    readonly given.bounded_barrier _delete = new();
    Guid _duringDelete;

    void Establish()
    {
        HasOutstandingJobs(new JobState { Id = _jobId, Status = JobStatus.Running, Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, []) });
        _eventSequence.GetTailSequenceNumber().Returns(Task.FromResult<EventSequenceNumber>(100UL));
        _jobsManager.Configure().Delete(_jobId).Returns(_ => _delete.Arrive());
    }

    async Task Because()
    {
        var earlier = await _observer.ResumingCatchUp(_jobId);
        var fastForwarding = _observer.FastForward();
        await _delete.WaitUntilReached();
        _duringDelete = await _observer.ResumingCatchUp(_jobId);
        _delete.Release();
        await given.bounded_barrier.Finishes(fastForwarding);
        await _observer.RegisterResumedCatchingUpPartitions(_jobId, earlier, [(Key)"P"]);
    }

    [Fact] void should_not_issue_a_generation_while_the_skipped_jobs_are_being_dropped() => _duringDelete.ShouldEqual(Guid.Empty);
    [Fact] void should_not_hold_the_partition_for_the_deleted_job() => _gatedStateStorage.State.CatchingUpPartitions.ShouldNotContain((Key)"P");
}
