// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instance_by_key;

public class and_the_schema_declares_initialization_state : given.an_instance_with_initialization_state
{
    void Establish()
    {
        var schema = JsonSchema.FromJson("""
            {"type":"object","properties":{"name":{"type":"string"},"__initialized":{"type":"boolean"}}}
            """);
        _readModelDefinition = _readModelDefinition with { Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema } };
        _readModel.GetDefinition().Returns(_readModelDefinition);
    }

    async Task Because()
    {
        var response = await _service.GetInstanceByKey(new() { EventStore = "test-store", Namespace = "test-namespace", ReadModelIdentifier = _readModelDefinition.Identifier, ReadModelKey = "first" });
        _document = JsonNode.Parse(response.ReadModel)!.AsObject();
    }

    [Fact] void should_preserve_the_declared_property() => _document[WellKnownProperties.ReadModelInstanceInitialized]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_preserve_the_read_model_data() => _document["name"]!.GetValue<string>().ShouldEqual("First");
}
