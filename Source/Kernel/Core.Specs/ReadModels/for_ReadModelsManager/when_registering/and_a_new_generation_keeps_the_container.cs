// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_a_new_generation_keeps_the_container : given.a_read_models_manager
{
    static readonly ReadModelDefinition _first = DefinitionFor("model", "Model") with
    {
        Schemas = new Dictionary<ReadModelGeneration, Schemas.JsonSchema> { [ReadModelGeneration.First] = new() }
    };
    static readonly ReadModelDefinition _second = _first with
    {
        Schemas = new Dictionary<ReadModelGeneration, Schemas.JsonSchema> { [(ReadModelGeneration)2] = new() }
    };

    async Task Establish() => await _manager.RegisterSingle(_first);

    async Task Because() => await _manager.RegisterSingle(_second);

    [Fact] async Task should_store_the_new_generation() => (await _manager.GetDefinitions()).ShouldContainOnly(_second);
    [Fact] async Task should_propagate_the_new_generation() => await _readModelGrain.Received(1).SetDefinition(_second);
    [Fact] void should_persist_the_new_definition() => _silo.StorageManager.GetStorageStats(typeof(ReadModelsManager).FullName)!.Writes.ShouldEqual(2);
}
