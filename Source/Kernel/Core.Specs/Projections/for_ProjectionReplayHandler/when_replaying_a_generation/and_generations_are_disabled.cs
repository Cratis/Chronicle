// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Projections.Generations;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_replaying_a_generation;

public class and_generations_are_disabled : given.a_generation_bound_replay_handler
{
    void Establish() => Unbind();

    Task Because() => _handler.BeginReplayFor(_observerDetails);

    [Fact] void should_look_up_the_current_projection() => _projections.Received(1).GetCurrent(_observerDetails.Key.EventStore, _observerDetails.Key.Namespace, _observerDetails.Key.ObserverId);
    [Fact] void should_not_admit_a_grant() => _generations.DidNotReceive().Admit(Arg.Any<EventStoreName>(), Arg.Any<GenerationExecution>());
    [Fact] void should_not_build_a_generation_pipeline() => _generationPipelines.DidNotReceive().GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Engine.IProjection>(), Arg.Any<GenerationSinkContext>());
}
