// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_preparing_steps;

public class and_steps_carry_the_replay_epoch : given.a_replay_observer_job
{
    IImmutableList<JobStepDetails> _steps;

    void Establish()
    {
        _request = _request with { ObserverType = ObserverType.Reactor };
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys((Key)"partition-1", (Key)"partition-2"));
    }

    async Task Because() => _steps = await _job.PrepareStepsForTesting(_request);

    [Fact] void should_stamp_every_step_with_the_job_id() => _steps.All(_ => ((HandleEventsForPartitionArguments)_.Request).ReplayEpoch == new ReplayEpoch(_jobId.Value)).ShouldBeTrue();
}
