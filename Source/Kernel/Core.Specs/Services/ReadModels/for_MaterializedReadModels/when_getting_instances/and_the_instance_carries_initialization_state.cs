// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.ReadModels.for_MaterializedReadModels.when_getting_instances;

public class and_the_instance_carries_initialization_state : for_ReadModels.given.an_instance_with_initialization_state
{
    async Task Because()
    {
        var service = new MaterializedReadModels(_grainFactory, _storage, _complianceHelper, NullLogger<MaterializedReadModels>.Instance);
        var response = await service.GetInstances(new() { EventStore = "test-store", Namespace = "test-namespace", ReadModel = _readModelDefinition.Identifier });
        _document = JsonNode.Parse(response.Instances.Single())!.AsObject();
    }

    [Fact] void should_not_expose_initialization_state() => _document.ContainsKey(WellKnownProperties.ReadModelInstanceInitialized).ShouldBeFalse();
    [Fact] void should_preserve_the_read_model_data() => _document["name"]!.GetValue<string>().ShouldEqual("First");
    [Fact] void should_not_remove_initialization_from_the_sink_instance() => _values[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(true);
}
