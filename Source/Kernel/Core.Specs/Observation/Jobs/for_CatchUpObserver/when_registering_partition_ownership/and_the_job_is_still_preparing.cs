// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_registering_partition_ownership;

/// <summary>
/// A job that is still preparing registers its own partitions once its steps are prepared.
/// </summary>
public class and_the_job_is_still_preparing : given.a_catch_up_observer_job
{
    static readonly Key _key1 = (Key)"partition-1";

    void Establish() => _job.SetStateForTesting(
        new(_request.ObserverKey, ObserverType.Reactor, _request.FromEventSequenceNumber, []),
        JobStatus.PreparingSteps,
        [PlannedPartitionStep(_key1)]);

    async Task Because() => await _job.RegisterPartitionOwnership();

    [Fact] async Task should_not_register_any_partitions() => await _observer.DidNotReceive().RegisterResumedCatchingUpPartitions(Arg.Any<JobId>(), Arg.Any<IEnumerable<Key>>());
}
