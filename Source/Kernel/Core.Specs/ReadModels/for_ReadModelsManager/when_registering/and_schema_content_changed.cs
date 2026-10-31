// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_schema_content_changed : given.a_read_models_manager
{
    ReadModelDefinition _definition;

    async Task Establish()
    {
        _definition = DefinitionFor("model", "Model") with
        {
            Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = new(JsonNode.Parse("{\"type\":\"object\"}")!.AsObject()) }
        };
        await _manager.Register([_definition]);
        _readModelGrain.ClearReceivedCalls();
    }

    async Task Because() => await _manager.Register([_definition with
    {
        Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = new(JsonNode.Parse("{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}")!.AsObject()) }
    }]);

    [Fact] void should_persist_the_change() => _silo.StorageManager.GetStorageStats(typeof(ReadModelsManager).FullName)!.Writes.ShouldEqual(2);
    [Fact] async Task should_set_the_changed_definition() => await _readModelGrain.Received(1).SetDefinition(Arg.Any<ReadModelDefinition>());
    [Fact] async Task should_reconcile_pipelines() => await _projectionsManagerGrain.Received(1).GetProjectionDefinitions();
}
