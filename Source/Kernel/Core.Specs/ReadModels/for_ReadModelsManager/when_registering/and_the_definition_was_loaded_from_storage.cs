// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_the_definition_was_loaded_from_storage : given.a_read_models_manager
{
    static readonly ReadModelDefinition _definition = DefinitionFor("model", "Model");

    void Establish()
    {
        _silo.StorageManager.GetStorage<ReadModelsManagerState>(typeof(ReadModelsManager).FullName).State.ReadModels = [_definition];
        _projectionsManagerGrain.GetProjectionDefinitions().Returns([ProjectionTargeting("projection", _definition.Identifier)]);
    }

    async Task Because() => await _manager.Register([_definition with { Schemas = new Dictionary<ReadModelGeneration, Schemas.JsonSchema>(), Indexes = [] }]);

    [Fact] void should_not_rewrite_equal_persisted_definitions() => _silo.StorageManager.GetStorageStats(typeof(ReadModelsManager).FullName)!.Writes.ShouldEqual(0);
    [Fact] async Task should_reconcile_the_read_model() => await _readModelGrain.Received(1).SetDefinition(Arg.Any<ReadModelDefinition>());
    [Fact] void should_reconcile_pipelines_from_a_previous_activation() => _projectionPipelines.Received(1).EvictFor(_eventStore, "default", "projection");
}
