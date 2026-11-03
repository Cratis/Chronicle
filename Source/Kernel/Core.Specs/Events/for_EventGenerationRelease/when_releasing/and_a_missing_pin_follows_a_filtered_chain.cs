// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.when_releasing;

public class and_a_missing_pin_follows_a_filtered_chain : given.a_release_boundary
{
    const string Original = """{"id":"1","status":"legacy"}""";

    void Establish()
    {
        var full = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"string"},"status":{"type":["string","null"]}}}""");
        var intermediate = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"string"}}}""");
        _pin = new(_pin.Id, 3);
        _definition = new(
            _pin.Id,
            EventTypeOwner.Server,
            false,
            [new(1, full), new(2, intermediate), new(3, full)],
            [new(1, 2, [], new JsonObject(), new JsonObject()), new(2, 3, [], new JsonObject { ["status"] = new JsonObject { ["$defaultValue"] = "pending" } }, new JsonObject())]);
        _schemas = new Dictionary<EventType, EventTypeSchema> { [_pin] = new(_pin, EventTypeOwner.Server, EventTypeSource.Code, full) };
        _event = new(EventContext.Empty with { EventType = new(_pin.Id, 1), AppendedGeneration = 1 }, _converter.ToExpandoObject(JsonNode.Parse(Original)!.AsObject(), full))
        {
            GenerationalContent = new Dictionary<int, string> { [1] = Original }
        };
        _metadata.ReleaseStrict(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>()).Returns(call => call.ArgAt<JsonObject>(4));
        _release = new(_storage, new EventCompliance(_metadata, _converter, new OwnGenerationSchemaCache()), new EventTypeMigrations(_storage, _converter), _metadata, _converter, new OwnGenerationSchemaCache());
    }

    async Task Because() => _result = await _release.Release(_event.Context.EventStore, [_pin], _schemas, [_event]);

    [Fact] void should_release_the_defaulted_value() => ((IDictionary<string, object?>)_result[0].Content)["status"].ShouldEqual("pending");
    [Fact] void should_not_persist_delivery_time_migrations() => _storage.GetEventStore(_event.Context.EventStore).DidNotReceive().GetNamespace(Arg.Any<EventStoreNamespaceName>());
    [Fact] void should_keep_the_original_generation_bytes() => _event.GenerationalContent[1].ShouldEqual(Original);
    [Fact] void should_not_claim_a_durable_hash() => _result[0].Context.Hash.ShouldEqual(EventHash.NotSet);
}
