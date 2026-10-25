// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_a_resumed_catch_up_registers_its_partitions;

/// <summary>
/// A partition that failed after the job planned its steps belongs to its recovery episode and is not held (#5406).
/// </summary>
public class and_a_partition_failed_before_it_was_accepted : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly JobId _jobId = JobId.New();

    void Establish() => _failedPartitionsState.RegisterAttempt((Key)"P", 42UL, ["failed"], string.Empty);

    async Task Because()
    {
        var generation = await _observer.ResumingCatchUp(_jobId);
        await _observer.RegisterResumedCatchingUpPartitions(_jobId, generation, [(Key)"P", (Key)"Q"]);
    }

    [Fact] void should_not_hold_the_failed_partition() => _stateStorage.State.CatchingUpPartitions.ShouldNotContain((Key)"P");
    [Fact] void should_hold_the_other_partition() => _stateStorage.State.CatchingUpPartitions.ShouldContain((Key)"Q");
}
