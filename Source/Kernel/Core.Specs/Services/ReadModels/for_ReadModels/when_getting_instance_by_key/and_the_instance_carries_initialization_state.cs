// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instance_by_key;

public class and_the_instance_carries_initialization_state : given.an_instance_with_initialization_state
{
    GetInstanceByKeyResponse _response;

    async Task Because()
    {
        _response = await _service.GetInstanceByKey(new() { EventStore = "test-store", Namespace = "test-namespace", ReadModelIdentifier = _readModelDefinition.Identifier, ReadModelKey = "first" });
        _document = JsonNode.Parse(_response.ReadModel)!.AsObject();
    }

    [Fact] void should_not_expose_initialization_state() => _document.ContainsKey(WellKnownProperties.ReadModelInstanceInitialized).ShouldBeFalse();
    [Fact] void should_preserve_the_read_model_data() => _document["name"]!.GetValue<string>().ShouldEqual("First");
    [Fact] void should_not_remove_initialization_from_the_sink_instance() => _values[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(true);
    [Fact] void should_keep_the_response_watermark() => _response.LastHandledEventSequenceNumber.ShouldEqual(42UL);
}
