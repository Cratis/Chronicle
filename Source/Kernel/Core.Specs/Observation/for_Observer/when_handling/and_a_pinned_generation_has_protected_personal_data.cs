// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_a_pinned_generation_has_protected_personal_data : given.an_observer
{
    readonly EventType _pin = new("person-registered", 2);
    AppendedEvent _appended;
    AppendedEvent _delivered;

    async Task Establish()
    {
        var firstSchema = CreateSchema("name");
        var pinnedSchema = CreateSchema("fullName");
        var schemas = new[]
        {
            new EventTypeSchema(new(_pin.Id, 1), EventTypeOwner.Client, EventTypeSource.Code, firstSchema),
            new EventTypeSchema(_pin, EventTypeOwner.Client, EventTypeSource.Code, pinnedSchema)
        };
        _eventTypesStorage.GetFor(Arg.Any<IEnumerable<EventType>>()).Returns(call => schemas.Where(schema => call.Arg<IEnumerable<EventType>>().Contains(schema.Type)));
        _eventTypesStorage.HasFor(_pin.Id, _pin.Generation).Returns(true);
        _eventTypesStorage.GetDefinition(_pin.Id).Returns(new EventTypeDefinition(_pin.Id, EventTypeOwner.Client, false, [new(1, firstSchema), new(2, pinnedSchema)], []));
        var metadata = Substitute.For<IJsonSchemaMetadataManager>();
        metadata.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>()).Returns(call =>
        {
            var content = (JsonObject)call.Arg<JsonObject>().DeepClone();
            foreach (var property in content.ToArray())
            {
                content[property.Key] = "Ada Lovelace";
            }

            return content;
        });
        var converter = new ExpandoObjectConverter(Substitute.For<ITypeFormats>());
        var compliance = new EventCompliance(metadata, converter);
        var release = new EventGenerationRelease(_storage, compliance, Substitute.For<IEventTypeMigrations>(), metadata, converter);
        _eventCompliance.Release(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>()).Returns(call => compliance.Release(call.Arg<IEnumerable<AppendedEvent>>(), call.Arg<IDictionary<EventType, EventTypeSchema>>()));
        _eventGenerationRelease.Release(Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>(), Arg.Any<IEnumerable<AppendedEvent>>()).Returns(call => release.Release(call.Arg<EventStoreName>(), call.Arg<IEnumerable<EventType>>(), call.Arg<IDictionary<EventType, EventTypeSchema>>(), call.Arg<IEnumerable<AppendedEvent>>()));
        _appended = new(EventContext.Empty with { EventType = new(_pin.Id, 1), SequenceNumber = 42UL, Subject = "person", AppendedGeneration = 1 }, converter.ToExpandoObject(new JsonObject { ["name"] = "ciphertext" }, firstSchema))
        {
            GenerationalContent = new Dictionary<int, string> { [1] = "{\"name\":\"ciphertext\"}", [2] = "{\"fullName\":\"ciphertext\"}" },
            GenerationalHashes = new Dictionary<int, EventHash> { [2] = "pinned-hash" }
        };
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(call =>
        {
            _delivered = call.Arg<IEnumerable<AppendedEvent>>().Single();
            return ObserverSubscriberResult.Ok(42UL);
        });
        await _observer.SubscribeWithGenerationDelivery<IObserverSubscriber>(ObserverType.Reactor, [_pin], SiloAddress.Zero, EventGenerationDelivery.Pinned);
    }

    async Task Because() => await _observer.Handle("person", [_appended]);

    [Fact] void should_deliver_released_personal_data() => ((IDictionary<string, object?>)_delivered.Content)["fullName"].ShouldEqual("Ada Lovelace");
    [Fact] void should_deliver_the_pinned_generation() => _delivered.Context.EventType.ShouldEqual(_pin);
    [Fact] void should_deliver_the_pinned_hash() => _delivered.Context.Hash.ShouldEqual((EventHash)"pinned-hash");
    [Fact] void should_preserve_the_appended_generation() => _delivered.Context.AppendedGeneration!.Value.ShouldEqual(1U);
    [Fact] void should_leave_stored_generations_protected() => _delivered.GenerationalContent[2].ShouldEqual("{\"fullName\":\"ciphertext\"}");

    static JsonSchema CreateSchema(string property)
    {
        var schema = JsonSchema.FromJson($"{{\"type\":\"object\",\"properties\":{{\"{property}\":{{\"type\":\"string\"}}}}}}");
        schema.Properties[property].ExtensionData = new Dictionary<string, object?>
        {
            [ComplianceJsonSchemaExtensions.ComplianceKey] = new[] { new ComplianceSchemaMetadata("PII", string.Empty) }
        };

        return schema;
    }
}
