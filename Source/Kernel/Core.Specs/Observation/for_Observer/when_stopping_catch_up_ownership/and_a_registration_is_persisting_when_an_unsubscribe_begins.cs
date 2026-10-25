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
/// A registration whose write is in flight when the stop begins must not lower the blanket hold or keep its holds afterwards (#5406).
/// </summary>
public class and_a_registration_is_persisting_when_an_unsubscribe_begins : given.an_observer_with_a_gated_state_write
{
    static readonly JobId _jobId = JobId.New();
    readonly given.bounded_barrier _stop = new();
    bool _isPreparingCatchup;

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
        var generation = await _observer.ResumingCatchUp(_jobId);
        _gatedStateStorage.SuspendNextWrite = true;
        var registration = _observer.RegisterResumedCatchingUpPartitions(_jobId, generation, [(Key)"P"]);
        await given.bounded_barrier.Finishes(_gatedStateStorage.WriteStarted.Task);
        var unsubscribing = _observer.Unsubscribe();
        await _stop.WaitUntilReached();
        _gatedStateStorage.ReleaseWrite.SetResult();
        await given.bounded_barrier.Finishes(registration);
        _isPreparingCatchup = await _observer.IsPreparingCatchup();
        _stop.Release();
        await given.bounded_barrier.Finishes(unsubscribing);
    }

    [Fact] void should_keep_the_blanket_hold_while_stopping() => _isPreparingCatchup.ShouldBeTrue();
}
