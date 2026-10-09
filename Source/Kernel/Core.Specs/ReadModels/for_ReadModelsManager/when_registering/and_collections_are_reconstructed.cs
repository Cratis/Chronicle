// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_collections_are_reconstructed : given.a_read_models_manager
{
    ReadModelDefinition _definition;

    async Task Establish()
    {
        _definition = DefinitionFor("model", "Model") with
        {
            Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = new(JsonNode.Parse("{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}")!.AsObject()) },
            Indexes = [new("name"), new("other")]
        };
        await _manager.Register([_definition]);
        _readModelGrain.ClearReceivedCalls();
    }

    async Task Because() => await _manager.Register([_definition with
    {
        Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = new(JsonNode.Parse("{\"properties\":{\"name\":{\"type\":\"string\"}},\"type\":\"object\"}")!.AsObject()) },
        Indexes = [new("other"), new("name")]
    }]);

    [Fact] void should_write_the_global_definitions_only_once() => _silo.StorageManager.GetStorageStats(typeof(ReadModelsManager).FullName)!.Writes.ShouldEqual(1);
    [Fact] async Task should_not_set_the_definition_again() => await _readModelGrain.DidNotReceive().SetDefinition(Arg.Any<ReadModelDefinition>());
    [Fact] async Task should_not_query_projections_for_eviction() => await _projectionsManagerGrain.DidNotReceive().GetProjectionDefinitions();
}
