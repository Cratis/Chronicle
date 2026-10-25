// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_replaying_a_generation;

public class and_the_replay_begins : given.a_generation_bound_replay_handler
{
    Result<ICanHandleReplayForObserver.Error> _result;

    async Task Because() => _result = await _handler.BeginReplayFor(_observerDetails);

    [Fact] void should_succeed() => _result.TryGetError(out _).ShouldBeFalse();
    [Fact] void should_build_the_pipeline_with_the_admitted_context() => _generationPipelines.Received(1).GetFor(_observerDetails.Key.EventStore, _observerDetails.Key.Namespace, _generationProjection, _context);
    [Fact] void should_begin_a_bulk_load_on_the_generation_pipeline() => _generationPipeline.Received(1).BeginBulk();
    [Fact] void should_not_begin_a_replay_on_the_generation_pipeline() => _generationPipeline.DidNotReceive().BeginReplay(Arg.Any<ReplayContext>());
    [Fact] void should_not_establish_a_replay_context() => _replayContexts.DidNotReceive().Establish(Arg.Any<ReadModelType>(), Arg.Any<ReadModelContainerName>());
    [Fact] void should_not_apply_a_retention_policy() => _readModelReplayManager.DidNotReceive().ApplyRetentionPolicy(Arg.Any<int>());
    [Fact] void should_not_look_up_the_current_projection() => _projections.DidNotReceive().GetCurrent(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionId>());
    [Fact] void should_not_build_a_legacy_pipeline() => _projectionPipelineManager.DidNotReceive().GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Engine.IProjection>());
}
