// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_eviction_previously_failed : given.a_read_models_manager
{
    static readonly ReadModelDefinition _original = DefinitionFor("model", "Model");
    static readonly ReadModelDefinition _updated = _original with { ContainerName = "new-container" };
    Exception _error;

    async Task Establish()
    {
        await _manager.Register([_original]);
        _projectionsManagerGrain.GetProjectionDefinitions().Returns(Task.FromException<IEnumerable<ProjectionDefinition>>(new Exception("Unavailable")));
        _error = await Catch.Exception(() => _manager.Register([_updated]));
        _projectionsManagerGrain.GetProjectionDefinitions().Returns([ProjectionTargeting("projection", _updated.Identifier)]);
    }

    async Task Because() => await _manager.Register([_updated with { Schemas = new Dictionary<ReadModelGeneration, Schemas.JsonSchema>(), Indexes = [] }]);

    [Fact] void should_surface_the_failed_registration() => _error.ShouldNotBeNull();
    [Fact] async Task should_retry_eviction() => await _projectionsManagerGrain.Received(2).GetProjectionDefinitions();
    [Fact] void should_evict_the_pipeline() => _projectionPipelines.Received(1).EvictFor(_eventStore, "default", "projection");
    [Fact] void should_not_repeat_the_global_write() => _silo.StorageManager.GetStorageStats(typeof(ReadModelsManager).FullName)!.Writes.ShouldEqual(2);
}
