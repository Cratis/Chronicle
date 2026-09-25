// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_serialized_initial_state;

public class and_the_initial_state_contains_nested_json : Specification
{
    const string Payload = """{"Outer":{"Name":"upper","name":"lower","Missing":null,"Items":[{"Code":"A"},[1,true,"value"],null]}}""";

    ReadModelScenario<SeededJsonReadModel> _scenario;
    SeededJsonReadModel _result;
    EventSourceId _id;
    Guid _idGuid;
    JsonObject _payload;

    void Establish()
    {
        _idGuid = Guid.NewGuid();
        _id = new EventSourceId(_idGuid);
        _payload = JsonNode.Parse(Payload)!.AsObject();
        _scenario = new ReadModelScenario<SeededJsonReadModel>(new SeededJsonReadModel(_idGuid, "seeded", _payload));
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_id).Events(new SeededJsonRenamed("renamed"));
        _result = _scenario.Instance!;
    }

    [Fact] void should_complete_projection() => _result.ShouldNotBeNull();
    [Fact] void should_apply_the_event_to_the_sibling_property() => _result.Name.ShouldEqual("renamed");
    [Fact] void should_preserve_the_exact_json() => _result.Payload.ToJsonString().ShouldEqual(Payload);
    [Fact] void should_preserve_the_upper_case_key() => _result.Payload["Outer"]!["Name"]!.GetValue<string>().ShouldEqual("upper");
    [Fact] void should_preserve_the_lower_case_key() => _result.Payload["Outer"]!["name"]!.GetValue<string>().ShouldEqual("lower");
    [Fact] void should_retain_the_null_valued_key() => _result.Payload["Outer"]!.AsObject().ContainsKey("Missing").ShouldBeTrue();
    [Fact] void should_preserve_the_nested_array() => _result.Payload["Outer"]!["Items"]![1]!.ToJsonString().ShouldEqual("[1,true,\"value\"]");
    [Fact] void should_preserve_the_null_array_element() => _result.Payload["Outer"]!["Items"]![2].ShouldBeNull();
    [Fact] void should_preserve_the_event_source_id() => _result.Id.ShouldEqual(_idGuid);
    [Fact] void should_preserve_the_payload_in_the_sink() => _scenario.InstanceForEventSourceId(_id)!.Payload.ToJsonString().ShouldEqual(Payload);
    [Fact] void should_apply_the_event_in_the_sink() => _scenario.InstanceForEventSourceId(_id)!.Name.ShouldEqual("renamed");
    [Fact] void should_not_mutate_the_original_payload() => _payload.ToJsonString().ShouldEqual(Payload);
}
