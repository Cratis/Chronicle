// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_preparing_steps;

public class and_observer_is_a_projection : given.a_replay_observer_job
{
    IImmutableList<JobStepDetails> _steps;

    async Task Because() => _steps = await _job.PrepareStepsForTesting(_request);

    [Fact] void should_create_one_step() => _steps.Count.ShouldEqual(1);
    [Fact] void should_use_ordered_observer_step() => _steps[0].Type.ShouldEqual(typeof(IHandleEventsForObserver));
    [Fact] void should_not_enumerate_partitions() => _keyIndexes.DidNotReceive().GetFor(Arg.Any<ObserverKey>());
    [Fact] void should_request_replay_observation_state() => ((HandleEventsForObserverArguments)_steps[0].Request).EventObservationState.ShouldEqual(EventObservationState.Replay);
}
