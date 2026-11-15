// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition.when_preparing_recovery_steps;

public class and_the_failure_belongs_to_a_replay : given.a_retry_failed_partition_job
{
    static readonly ReplayEpoch _epoch = new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000004"));
    IImmutableList<JobStepDetails> _steps;

    void Establish() => _request = _request with { ReplayEpoch = _epoch };

    async Task Because() => _steps = await _job.PrepareRecoverySteps(_request);

    [Fact] void should_carry_the_replay_epoch_to_the_step() => ((HandleEventsForPartitionArguments)_steps[0].Request).ReplayEpoch.ShouldEqual(_epoch);
    [Fact] void should_still_not_present_the_redelivery_as_a_replay() => ((HandleEventsForPartitionArguments)_steps[0].Request).EventObservationState.ShouldEqual(EventObservationState.None);
}
