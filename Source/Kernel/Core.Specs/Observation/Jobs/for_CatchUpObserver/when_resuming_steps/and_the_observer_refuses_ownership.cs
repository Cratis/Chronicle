// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_resuming_steps;

public class and_the_observer_refuses_ownership : given.a_catch_up_observer_job
{
    void Establish()
    {
        _job.SetStateForTesting(new(_request.ObserverKey, ObserverType.Reactor, _request.FromEventSequenceNumber, []), JobStatus.Running, []);
        _observer.ResumingCatchUp(_jobId).Returns(Guid.Empty);
    }

    async Task Because() => await _job.ResumeHookForTesting();

    [Fact] async Task should_not_queue_the_registration_of_its_partitions() => await _self.DidNotReceive().RegisterPartitionOwnership(Arg.Any<Guid>());
}
