// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_a_resumed_catch_up_registers_its_partitions;

public class and_it_registers_twice : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly JobId _jobId = JobId.New();

    async Task Because()
    {
        var generation = await _observer.ResumingCatchUp(_jobId);
        await _observer.RegisterResumedCatchingUpPartitions(_jobId, generation, [(Key)"P"]);
        await _observer.RegisterResumedCatchingUpPartitions(_jobId, generation, [(Key)"P"]);
    }

    [Fact] void should_hold_the_partition_once() => _stateStorage.State.CatchingUpPartitions.Count.ShouldEqual(1);
}
