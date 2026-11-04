// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_an_intermediate_generation_removed_a_property : given.an_event_sequence
{
    JsonObject _content;
    IDictionary<EventTypeGeneration, ExpandoObject> _stored;

    void Establish()
    {
        _content = JsonNode.Parse("""{"id":"1","status":"legacy"}""")!.AsObject();
        var full = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"string"},"status":{"type":["string","null"]}}}""");
        var intermediate = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"string"}}}""");
        var definition = new EventTypeDefinition(
            _eventType.Id,
            EventTypeOwner.Server,
            false,
            [new(1, full), new(2, intermediate), new(3, full)],
            [new(1, 2, [], new JsonObject(), new JsonObject()), new(2, 3, [], new JsonObject { ["status"] = new JsonObject { ["$defaultValue"] = "pending" } }, new JsonObject())]);
        _eventTypesStorage.GetDefinition(_eventType.Id).Returns(definition);
        _eventTypesStorage.GetFor(_eventType.Id, _eventType.Generation).Returns(new EventTypeSchema(_eventType, EventTypeOwner.Server, EventTypeSource.Code, full));
        _complianceManager.Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>()).Returns(call => call.ArgAt<JsonObject>(4));
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var migrations = new EventTypeMigrations(_storage, converter);
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>()).Returns(call => converter.ToExpandoObject(call.ArgAt<JsonObject>(0), call.ArgAt<JsonSchema>(1)));
        _eventTypeMigrations.MigrateToAllGenerations(Arg.Any<EventStoreName>(), Arg.Any<EventType>(), Arg.Any<JsonObject>(), Arg.Any<ExpandoObject>()).Returns(call => migrations.MigrateToAllGenerations(call.ArgAt<EventStoreName>(0), call.ArgAt<EventType>(1), call.ArgAt<JsonObject>(2), call.ArgAt<ExpandoObject>(3)));
    }

    async Task Because()
    {
        await _eventSequence.Append(EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, _eventType, _content, CorrelationId.New(), [], Identity.System, [], ConcurrencyScope.None);
        _stored = (IDictionary<EventTypeGeneration, ExpandoObject>)_eventSequenceStorage.ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(IEventSequenceStorage.Append)).GetArguments()[11]!;
    }

    [Fact] void should_store_the_original_generation() => ((IDictionary<string, object?>)_stored[1])["status"].ShouldEqual("legacy");
    [Fact] void should_store_the_defaulted_third_generation() => ((IDictionary<string, object?>)_stored[3])["status"].ShouldEqual("pending");
    [Fact] void should_not_change_the_callers_content() => _content["status"]!.GetValue<string>().ShouldEqual("legacy");
}
