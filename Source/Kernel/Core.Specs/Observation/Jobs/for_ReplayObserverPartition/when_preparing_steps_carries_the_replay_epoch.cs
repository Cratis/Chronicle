// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition;

public class when_preparing_steps_carries_the_replay_epoch : given.a_partition_replay_job
{
    IImmutableList<JobStepDetails> _steps;

    void Establish() => _request = _request with { ObserverType = ObserverType.Reactor };

    async Task Because() => _steps = await _job.PrepareStepsForTesting(_request);

    [Fact] void should_stamp_the_step_with_the_job_id() => ((HandleEventsForPartitionArguments)_steps[0].Request).ReplayEpoch.ShouldEqual(new ReplayEpoch(Guid.Parse("55555555-5555-5555-5555-555555555555")));
}
