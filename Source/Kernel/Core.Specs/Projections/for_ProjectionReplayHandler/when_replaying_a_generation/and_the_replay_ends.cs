// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_replaying_a_generation;

public class and_the_replay_ends : given.a_generation_bound_replay_handler
{
    Result<ICanHandleReplayForObserver.Error> _result;

    async Task Because() => _result = await _handler.EndReplayFor(_observerDetails);

    [Fact] void should_succeed() => _result.TryGetError(out _).ShouldBeFalse();
    [Fact] void should_end_the_bulk_load_on_the_generation_pipeline() => _generationPipeline.Received(1).EndBulk();
    [Fact] void should_not_end_a_replay_on_the_generation_pipeline() => _generationPipeline.DidNotReceive().EndReplay(Arg.Any<ReplayContext>());
    [Fact] void should_not_leave_a_replay() => _generationPipeline.DidNotReceive().LeaveReplay();
    [Fact] void should_not_read_a_replay_context() => _replayContexts.DidNotReceive().TryGet(Arg.Any<ReadModelIdentifier>());
    [Fact] void should_not_evict_a_replay_context() => _replayContexts.DidNotReceive().Evict(Arg.Any<ReadModelIdentifier>());
    [Fact] void should_not_signal_the_replay_manager() => _readModelReplayManager.DidNotReceive().Replayed(Arg.Any<ObserverId>(), Arg.Any<ReplayContext>());
    [Fact] void should_not_build_a_legacy_pipeline() => _projectionPipelineManager.DidNotReceive().GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Engine.IProjection>());
}
