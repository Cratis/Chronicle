// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_revised_protected_values_at_a_different_pin : given.a_pinned_subscriber
{
    AppendedEvent _selected;

    async Task RegisterTypedSchemas()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{
              "amount":{"type":"integer","compliance":[{"metadataType":"PII","details":""}]},
              "enabled":{"type":"boolean","compliance":[{"metadataType":"PII","details":""}]},
              "profile":{"type":"object","properties":{"name":{"type":"string"}},"compliance":[{"metadataType":"PII","details":""}]}}}
            """);
        var definition = new EventTypeDefinition("event-type-1", EventTypeOwner.Client, false, [new(1, _schema), new(2, _schema)], []);
        foreach (var store in new[] { "Admin", "Lobby" })
        {
            var eventTypes = _storage.GetEventStore(store).EventTypes;
            eventTypes.GetDefinition(definition.Id).Returns(definition);
            foreach (var generation in new uint[] { 1, 2 })
            {
                eventTypes.GetFor(definition.Id, generation).Returns(new EventTypeSchema(new(definition.Id, generation), EventTypeOwner.Client, EventTypeSource.Code, _schema));
            }
        }
    }

    async Task Establish()
    {
        await RegisterTypedSchemas();
        _metadata.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(call => ReleaseProtectedValues(call.Arg<JsonObject>()));
        _metadata.ReleaseStrict(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(call => ReleaseProtectedValues(call.Arg<JsonObject>()));
        dynamic protectedContent = new ExpandoObject();
        protectedContent.amount = "sealed-integer";
        protectedContent.enabled = "sealed-boolean";
        protectedContent.profile = "sealed-object";
        const string revision = "{\"amount\":\"sealed-integer\",\"enabled\":\"sealed-boolean\",\"profile\":\"sealed-object\"}";
        var original = revision.Replace("sealed-integer", "sealed-original-integer", StringComparison.Ordinal);
        var stored = _events[0] with
        {
            Content = protectedContent,
            OriginalContent = original,
            GenerationalContent = new Dictionary<int, string> { [1] = original },
            RevisedGeneration = 2,
            Revisions = [new(2, CorrelationId.NotSet, [], Concepts.Identities.Identity.System, DateTimeOffset.UnixEpoch, revision)]
        };
        _events = [stored with { Context = stored.Context with { ObservationState = EventObservationState.Replay } }];
        _storage.GetEventStore("Admin").GetNamespace(EventStoreNamespaceName.Default).GetEventSequence("outbox").GetEventAt(42UL).Returns(stored);
    }

    async Task Because()
    {
        var source = _storage.GetEventStore("Admin");
        _selected = await new EventStoreSubscriptionTargetGeneration(source.EventTypes, _storage.GetEventStore("Lobby").EventTypes, _generationRelease, "Admin", "Lobby", source.GetNamespace(EventStoreNamespaceName.Default).GetEventSequence("outbox"))
            .Release(_events[0], new Dictionary<EventTypeId, EventType> { ["event-type-1"] = new("event-type-1", 1) });
    }

    [Fact] void should_release_the_integer() => ((IDictionary<string, object?>)_selected.Content)["amount"].ShouldEqual(42);
    [Fact] void should_release_the_boolean() => ((IDictionary<string, object?>)_selected.Content)["enabled"].ShouldEqual(true);
    [Fact] void should_release_the_nested_object() => ((IDictionary<string, object?>)((IDictionary<string, object?>)_selected.Content)["profile"]!)["name"].ShouldEqual("Ada");
    [Fact] void should_not_use_json_elements() => ((IDictionary<string, object?>)_selected.Content).Values.Any(value => value is JsonElement).ShouldBeFalse();
    [Fact] void should_select_the_pin() => _selected.Context.EventType.ShouldEqual(new EventType("event-type-1", 1));
    [Fact] void should_preserve_the_dispatch_context() => _selected.Context.ShouldEqual(_events[0].Context with { EventType = _selected.Context.EventType, Hash = _selected.Context.Hash });

    static JsonObject ReleaseProtectedValues(JsonObject content)
    {
        var amount = content["amount"]!.GetValue<string>() switch
        {
            "sealed-integer" => 42,
            "sealed-original-integer" => 11,
            _ => throw new InvalidOperationException("Content has already been released.")
        };
        content["enabled"]!.GetValue<string>().ShouldEqual("sealed-boolean");
        content["profile"]!.GetValue<string>().ShouldEqual("sealed-object");
        return new JsonObject { ["amount"] = amount, ["enabled"] = true, ["profile"] = new JsonObject { ["name"] = "Ada" } };
    }
}
