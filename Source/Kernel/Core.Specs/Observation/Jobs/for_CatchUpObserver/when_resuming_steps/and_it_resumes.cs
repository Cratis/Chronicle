// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_resuming_steps;

/// <summary>
/// A resumed catch-up owns partitions again before any of its steps deliver: the observer is told synchronously, so live delivery is held back, and the exact partitions are registered in the job's next turn.
/// </summary>
public class and_it_resumes : given.a_catch_up_observer_job
{
    readonly Guid _generation = Guid.NewGuid();

    void Establish()
    {
        _job.SetStateForTesting(new(_request.ObserverKey, ObserverType.Reactor, _request.FromEventSequenceNumber, []), JobStatus.Running, []);
        _observer.ResumingCatchUp(_jobId).Returns(_generation);
    }

    async Task Because() => await _job.ResumeHookForTesting();

    [Fact] async Task should_tell_the_observer_the_catch_up_is_resuming() => await _observer.Received(1).ResumingCatchUp(_jobId);
    [Fact] async Task should_resume_the_catch_up_for_the_observer() => await _catchupServiceClient.Received(1).ResumeCatchupFor(Arg.Any<ObserverDetails>());
    [Fact] async Task should_queue_the_registration_of_its_partitions_in_its_own_next_turn() => await _self.Received(1).RegisterPartitionOwnership(_generation);
    [Fact] async Task should_not_register_partitions_inline() => await _observer.DidNotReceive().RegisterResumedCatchingUpPartitions(Arg.Any<JobId>(), Arg.Any<Guid>(), Arg.Any<IEnumerable<Key>>());
}
