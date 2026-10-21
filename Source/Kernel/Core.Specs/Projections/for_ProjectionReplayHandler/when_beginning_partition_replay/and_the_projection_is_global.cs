// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_beginning_partition_replay;

public class and_the_projection_is_global : given.a_projection_replay_handler_with_projection
{
    Exception _error;

    void Establish() => _projection.Scope.Returns(ProjectionScope.Global);

    async Task Because() => _error = await Catch.Exception(() => _handler.BeginReplayPartitionFor(_observerDetails, "partition"));

    [Fact] void should_refuse_a_persisted_partition_replay() => _error.ShouldBeOfExactType<GlobalProjectionReplayNotSupported>();
    [Fact] async Task should_not_resolve_a_pipeline() => await _projectionPipelineManager.DidNotReceive().GetFor(_observerDetails.Key.EventStore, _observerDetails.Key.Namespace, _projection);
}
