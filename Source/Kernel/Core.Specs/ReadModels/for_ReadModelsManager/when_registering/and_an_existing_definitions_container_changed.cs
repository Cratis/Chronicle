// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

/// <summary>
/// A projection's own re-registration is a no-op whenever its field mappings are unchanged, so a read model's
/// container name changing underneath an already-registered projection would otherwise never reach the engine -
/// the projection keeps writing to the container it was first built with until the silo restarts (#4569).
/// </summary>
public class and_an_existing_definitions_container_changed : given.a_read_models_manager
{
    static readonly ReadModelDefinition _first = DefinitionFor("some-read-model", "Display name");
    static readonly ReadModelDefinition _second = _first with { ContainerName = "a-different-container" };
    static readonly ProjectionId _targetingProjection = "some-projection";
    static readonly ProjectionId _unrelatedProjection = "some-unrelated-projection";

    async Task Establish()
    {
        _projectionsManagerGrain.GetProjectionDefinitions().Returns(
        [
            ProjectionTargeting(_targetingProjection, _first.Identifier),
            ProjectionTargeting(_unrelatedProjection, "some-other-read-model")
        ]);

        await _manager.Register([_first]);
    }

    async Task Because() => await _manager.Register([_second]);

    [Fact] void should_evict_the_pipeline_of_the_projection_targeting_the_changed_read_model() =>
        _projectionPipelines.Received(1).EvictFor(_eventStore, (EventStoreNamespaceName)"default", _targetingProjection);

    [Fact] void should_not_evict_the_pipeline_of_the_unrelated_projection() =>
        _projectionPipelines.DidNotReceive().EvictFor(_eventStore, (EventStoreNamespaceName)"default", _unrelatedProjection);
}
