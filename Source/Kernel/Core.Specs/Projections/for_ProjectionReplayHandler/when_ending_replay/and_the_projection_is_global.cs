// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_ending_replay;

public class and_the_projection_is_global : given.a_projection_replay_handler_with_projection
{
    Exception _error;

    void Establish() => _projection.Scope.Returns(ProjectionScope.Global);

    async Task Because() => _error = await Catch.Exception(() => _handler.EndReplayFor(_observerDetails));

    [Fact] void should_refuse_to_promote_a_namespace_local_rebuild() => _error.Message.ShouldEqual(GlobalProjectionReplay.RefusalReason);
    [Fact] void should_not_promote_the_shared_sink() => _projectionPipeline.DidNotReceive().EndReplay(Arg.Any<ReplayContext>());
    [Fact] async Task should_not_load_a_namespace_local_context() => await _replayContexts.DidNotReceive().TryGet(_readModelType.Identifier);
}
