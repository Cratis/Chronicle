// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_stopping_catch_up_ownership;

/// <summary>
/// No generation is issued between an unsubscribe's first and last await, and a registration from before or during the stop restores nothing (#5406).
/// </summary>
public class and_a_resume_interleaves_with_an_unsubscribe : given.an_observer_with_a_gated_state_write
{
    static readonly JobId _jobId = JobId.New();
    readonly given.bounded_barrier _stop = new();
    Guid _beforeStop;
    Guid _duringStop;
    Guid _afterStop;

    void Establish()
    {
        HasOutstandingJobs(new JobState { Id = _jobId, Status = JobStatus.Running, Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, []) });
        _jobsManager.Stop(_jobId).Returns(async _ =>
        {
            await _stop.Arrive();
            return Result<StopJobError>.Success();
        });
    }

    async Task Because()
    {
        _beforeStop = await _observer.ResumingCatchUp(_jobId);
        var unsubscribing = _observer.Unsubscribe();
        await _stop.WaitUntilReached();
        _duringStop = await _observer.ResumingCatchUp(_jobId);
        await _observer.RegisterResumedCatchingUpPartitions(_jobId, _beforeStop, [(Key)"P"]);
        await _observer.RegisterResumedCatchingUpPartitions(_jobId, _duringStop, [(Key)"P"]);
        _stop.Release();
        await given.bounded_barrier.Finishes(unsubscribing);
        await _observer.RegisterResumedCatchingUpPartitions(_jobId, _beforeStop, [(Key)"P"]);
        _afterStop = await _observer.ResumingCatchUp(_jobId);
    }

    [Fact] void should_not_issue_a_generation_while_stopping() => _duringStop.ShouldEqual(Guid.Empty);
    [Fact] void should_not_hold_the_partition_for_a_registration_from_before_or_during_the_stop() => _gatedStateStorage.State.CatchingUpPartitions.ShouldNotContain((Key)"P");
    [Fact] void should_readmit_the_job_once_it_is_resumed_after_the_stop() => _afterStop.ShouldNotEqual(Guid.Empty);
}
