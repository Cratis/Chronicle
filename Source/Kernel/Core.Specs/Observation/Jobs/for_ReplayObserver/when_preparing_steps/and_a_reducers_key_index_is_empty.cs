// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_preparing_steps;

public class and_a_reducers_key_index_is_empty : given.a_replay_observer_job
{
    IImmutableList<JobStepDetails> _steps;
    void Establish() => _request = _request with { ObserverType = ObserverType.Reducer };
    async Task Because() => _steps = await _job.PrepareStepsForTesting(_request);
    [Fact] void should_reuse_the_projections_ordered_walker() => _steps.Single().Type.ShouldEqual(typeof(IHandleEventsForObserver));
    [Fact] void should_carry_the_replay_identity() => ((HandleEventsForObserverArguments)_steps.Single().Request).ReducerReplayJobId.ShouldEqual(_jobId.Value);
    [Fact] void should_not_trust_an_incomplete_key_index() => _keyIndexes.DidNotReceive().GetFor(Arg.Any<ObserverKey>());
}
