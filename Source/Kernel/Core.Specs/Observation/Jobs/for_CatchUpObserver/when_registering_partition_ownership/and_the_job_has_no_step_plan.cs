// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_registering_partition_ownership;

/// <summary>
/// A job written before step plans were saved cannot say which partitions it owns. The blanket hold stays up until it concludes, which is stricter than registering a guess.
/// </summary>
public class and_the_job_has_no_step_plan : given.a_catch_up_observer_job
{
    void Establish() => _job.SetStateForTesting(new(_request.ObserverKey, ObserverType.Reactor, _request.FromEventSequenceNumber, []), JobStatus.Running, null);

    async Task Because() => await _job.RegisterPartitionOwnership();

    [Fact] async Task should_not_register_any_partitions() => await _observer.DidNotReceive().RegisterResumedCatchingUpPartitions(Arg.Any<JobId>(), Arg.Any<IEnumerable<Key>>());
}
