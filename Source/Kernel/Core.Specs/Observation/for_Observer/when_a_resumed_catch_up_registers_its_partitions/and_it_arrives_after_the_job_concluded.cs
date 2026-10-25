// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_a_resumed_catch_up_registers_its_partitions;

/// <summary>
/// A registration after conclusion holds nothing and raises nothing (#5406).
/// </summary>
public class and_it_arrives_after_the_job_concluded : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly JobId _jobId = JobId.New();
    bool _isPreparingCatchup;

    async Task Because()
    {
        var generation = await _observer.ResumingCatchUp(_jobId);
        await _observer.CaughtUp(_jobId, 41UL);
        await _observer.RegisterResumedCatchingUpPartitions(_jobId, generation, [(Key)"P"]);
        _isPreparingCatchup = await _observer.IsPreparingCatchup();
    }

    [Fact] void should_not_hold_the_partition() => _stateStorage.State.CatchingUpPartitions.ShouldNotContain((Key)"P");
    [Fact] void should_not_raise_the_preparing_flag() => _isPreparingCatchup.ShouldBeFalse();
}
