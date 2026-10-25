// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_registering_partition_ownership;

public class and_the_job_has_completed : given.a_catch_up_observer_job
{
    static readonly Key _key1 = (Key)"partition-1";

    void Establish() => _job.SetStateForTesting(
        new(_request.ObserverKey, ObserverType.Reactor, _request.FromEventSequenceNumber, []),
        JobStatus.Running,
        [PlannedPartitionStep(_key1)],
        isCompleted: true);

    async Task Because() => await _job.RegisterPartitionOwnership(_generation);

    [Fact] async Task should_not_register_any_partitions() => await _observer.DidNotReceive().RegisterResumedCatchingUpPartitions(Arg.Any<JobId>(), Arg.Any<Guid>(), Arg.Any<IEnumerable<Key>>());
}
