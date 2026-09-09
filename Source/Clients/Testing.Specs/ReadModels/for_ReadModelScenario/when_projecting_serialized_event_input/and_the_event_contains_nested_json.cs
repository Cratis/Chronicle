// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_serialized_event_input;

public class and_the_event_contains_nested_json : Specification
{
    const string Payload = """{"Outer":{"Name":"upper","name":"lower","Items":[{"Code":"A"},[1,true,"value"]]},"Enabled":false}""";

    ReadModelScenario<JsonPayloadReadModel> _scenario;
    JsonPayloadReadModel _result;
    EventSourceId _id;
    JsonObject _payload;

    void Establish()
    {
        _scenario = new ReadModelScenario<JsonPayloadReadModel>();
        _id = EventSourceId.New();
        _payload = JsonNode.Parse(Payload)!.AsObject();
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_id).Events(new JsonPayloadRecorded(_payload));
        _result = _scenario.Instance!;
    }

    [Fact] void should_complete_projection() => _result.ShouldNotBeNull();
    [Fact] void should_preserve_the_exact_json() => _result.Payload.ToJsonString().ShouldEqual(Payload);
    [Fact] void should_preserve_the_upper_case_key() => _result.Payload["Outer"]!["Name"]!.GetValue<string>().ShouldEqual("upper");
    [Fact] void should_preserve_the_lower_case_key() => _result.Payload["Outer"]!["name"]!.GetValue<string>().ShouldEqual("lower");
    [Fact] void should_preserve_the_nested_array() => _result.Payload["Outer"]!["Items"]![1]!.ToJsonString().ShouldEqual("[1,true,\"value\"]");
    [Fact] void should_preserve_the_event_source_id() => _result.Id.ShouldEqual(Guid.Parse(_id.Value));
    [Fact] void should_preserve_the_payload_in_the_sink() => _scenario.InstanceForEventSourceId(_id)!.Payload.ToJsonString().ShouldEqual(Payload);
    [Fact] void should_not_mutate_the_original_payload() => _payload.ToJsonString().ShouldEqual(Payload);
}
