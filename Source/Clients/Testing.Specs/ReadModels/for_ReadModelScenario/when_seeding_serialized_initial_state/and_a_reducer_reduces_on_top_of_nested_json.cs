// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_serialized_initial_state;

/// <summary>
/// Reducers receive the initial state as the typed instance rather than through the projection path's
/// serialized conversion, so the seeded payload reaches the reducer as the very same object.
/// </summary>
public class and_a_reducer_reduces_on_top_of_nested_json : Specification
{
    const string Payload = """{"Outer":{"Name":"upper","name":"lower","Missing":null,"Items":[{"Code":"A"},[1,true,"value"],null]}}""";

    ReadModelScenario<SeededJsonTally> _scenario;
    SeededJsonTally _result;
    EventSourceId _id;
    JsonObject _payload;

    void Establish()
    {
        var idGuid = Guid.NewGuid();
        _id = new EventSourceId(idGuid);
        _payload = JsonNode.Parse(Payload)!.AsObject();
        _scenario = new ReadModelScenario<SeededJsonTally>(new SeededJsonTally(idGuid, 100, _payload));
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_id).Events(new SeededJsonTallied());
        _result = _scenario.Instance!;
    }

    [Fact] void should_apply_the_event_on_top_of_the_seeded_state() => _result.Count.ShouldEqual(101);
    [Fact] void should_hand_the_reducer_the_seeded_payload_instance() => ReferenceEquals(_result.Payload, _payload).ShouldBeTrue();
    [Fact] void should_preserve_the_exact_json() => _result.Payload.ToJsonString().ShouldEqual(Payload);
}
