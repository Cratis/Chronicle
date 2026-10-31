// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.for_ProjectionsManager.given;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_ending_partition_replay;

public class and_the_global_definition_is_not_loaded : given.a_projection_replay_handler
{
    Exception _error;

    void Establish()
    {
        _eventStoreStorage.Projections.Has(_observerDetails.Key.ObserverId.Value).Returns(true);
        _eventStoreStorage.Projections.Get(_observerDetails.Key.ObserverId.Value).Returns(
            a_projections_manager_grain.CreateDefinition(_observerDetails.Key.ObserverId.Value, "read-model") with { Scope = ProjectionScope.Global });
    }

    async Task Because() => _error = await Catch.Exception(() => _handler.EndReplayPartitionFor(_observerDetails, "partition"));

    [Fact] void should_refuse_the_persisted_global_definition() => _error.ShouldBeOfExactType<GlobalProjectionReplayNotSupported>();
    [Fact] async Task should_not_resolve_a_pipeline() => await _projectionPipelineManager.DidNotReceiveWithAnyArgs().GetFor(default!, default!, default!);
}
