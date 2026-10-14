// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.given;

public class a_pinned_subscriber : a_subscriber_with_events
{
    protected EventStoreSubscriptionDefinition _definition;
    protected JsonSchema _schema;
    protected IJsonSchemaMetadataManager _metadata;
    protected ObserverSubscriberResult _result;

    void Establish()
    {
        _schema = JsonSchema.FromJson("{\"type\":\"object\",\"properties\":{\"message\":{\"type\":\"string\"}}}");
        _definition = new("subscription", "Admin", [new EventType("event-type-1", 1)]) { GenerationDelivery = EventGenerationDelivery.Pinned };
        _storage.GetEventStore("Lobby").EventStoreSubscriptions.GetAll().Returns(_ => [_definition]);
        RegisterGenerations("Admin", 1, 2);
        RegisterGenerations("Lobby", 1);
        _events = [_events[0] with
        {
            Context = _events[0].Context with { EventType = new("event-type-1", 2), AppendedGeneration = 2 },
            GenerationalContent = new Dictionary<int, string> { [1] = "{\"message\":\"ciphertext\"}", [2] = "{\"message\":\"new-generation\"}" },
            GenerationalHashes = new Dictionary<int, EventHash> { [1] = "first-hash", [2] = "second-hash" }
        }];
    }

    protected void RegisterGenerations(EventStoreName store, params uint[] generations)
    {
        var types = _storage.GetEventStore(store).EventTypes;
        types.GetDefinition("event-type-1").Returns(new EventTypeDefinition("event-type-1", EventTypeOwner.Client, false, generations.Select(generation => new EventTypeGenerationDefinition(generation, _schema)), []));
        foreach (var generation in generations)
        {
            types.HasFor("event-type-1", generation).Returns(true);
            types.GetFor("event-type-1", generation).Returns(new EventTypeSchema(new("event-type-1", generation), EventTypeOwner.Client, EventTypeSource.Code, _schema));
        }
    }

    protected override IEventGenerationRelease CreateGenerationRelease()
    {
        _metadata = Substitute.For<IJsonSchemaMetadataManager>();
        _metadata.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>()).Returns(call => ReleaseContent(call.ArgAt<JsonObject>(4)));
        _metadata.ReleaseStrict(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>()).Returns(call => ReleaseContent(call.ArgAt<JsonObject>(4)));
        var converter = new ExpandoObjectConverter(Substitute.For<ITypeFormats>());
        var migrations = Substitute.For<IEventTypeMigrations>();
        migrations.MigrateToAllGenerations(Arg.Any<EventTypeDefinition>(), Arg.Any<EventType>(), Arg.Any<JsonObject>(), Arg.Any<ExpandoObject>()).Returns(call => new Dictionary<EventTypeGeneration, ExpandoObject> { [1] = call.ArgAt<ExpandoObject>(3) });
        return new EventGenerationRelease(_storage, new EventCompliance(_metadata, converter), migrations, _metadata, converter);
    }

    protected EventType ForwardedType() => (EventType)_inboxSequence.ReceivedCalls().Last(call => call.GetMethodInfo().Name == "Append").GetArguments()[4]!;
    protected JsonObject ForwardedContent() => (JsonObject)_inboxSequence.ReceivedCalls().Last(call => call.GetMethodInfo().Name == "Append").GetArguments()[5]!;

    static JsonObject ReleaseContent(JsonObject content)
    {
        var released = (JsonObject)content.DeepClone();
        if (released["message"]?.GetValue<string>() == "ciphertext")
        {
            released["message"] = "released";
        }
        return released;
    }
}
