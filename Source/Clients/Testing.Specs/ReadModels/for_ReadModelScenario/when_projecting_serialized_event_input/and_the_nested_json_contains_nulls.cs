// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_serialized_event_input;

public class and_the_nested_json_contains_nulls : Specification
{
    const string Payload = """{"Outer":{"Name":"upper","name":"lower","Missing":null,"Items":[{"Value":null},null,1,true]}}""";

    ReadModelScenario<JsonPayloadReadModel> _scenario;
    JsonPayloadReadModel _result;
    EventSourceId _id;

    void Establish()
    {
        _scenario = new ReadModelScenario<JsonPayloadReadModel>();
        _id = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_id).Events(new JsonPayloadRecorded(JsonNode.Parse(Payload)!.AsObject()));
        _result = _scenario.Instance!;
    }

    [Fact] void should_complete_projection() => _result.ShouldNotBeNull();
    [Fact] void should_preserve_the_exact_json_including_nulls() => _result.Payload.ToJsonString().ShouldEqual(Payload);
    [Fact] void should_retain_the_null_valued_key() => _result.Payload["Outer"]!.AsObject().ContainsKey("Missing").ShouldBeTrue();
    [Fact] void should_preserve_the_null_array_element() => _result.Payload["Outer"]!["Items"]![1].ShouldBeNull();
    [Fact] void should_preserve_nulls_in_the_sink() => _scenario.InstanceForEventSourceId(_id)!.Payload.ToJsonString().ShouldEqual(Payload);
}
