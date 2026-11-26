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
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_releasing_pinned_history : an_observer
{
    protected readonly EventType _pin = new("person-registered", 2);
    protected AppendedEvent _appended;
    protected AppendedEvent _delivered;
    protected string _releasedValue = "Ada Lovelace";
    protected bool _includeRevision;

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
        _eventTypesStorage.GetAllGenerationsForEventType(Arg.Any<EventType>()).Returns(schemas);
        _eventTypesStorage.HasFor(_pin.Id, _pin.Generation).Returns(true);
        _eventTypesStorage.GetDefinition(_pin.Id).Returns(new EventTypeDefinition(_pin.Id, EventTypeOwner.Client, false, [new(1, firstSchema), new(2, pinnedSchema)], []));
        var metadata = Substitute.For<IJsonSchemaMetadataManager>();
        metadata.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>()).Returns(call =>
        {
            var content = (JsonObject)call.Arg<JsonObject>().DeepClone();
            foreach (var property in content.ToArray())
            {
                content[property.Key] = _releasedValue;
            }

            return content;
        });
        var converter = new ExpandoObjectConverter(Substitute.For<ITypeFormats>());
        var compliance = new EventCompliance(metadata, converter, new OwnGenerationSchemaCache());
        var release = new EventGenerationRelease(_storage, compliance, Substitute.For<IEventTypeMigrations>(), metadata, converter, new OwnGenerationSchemaCache());
        _eventCompliance.Release(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>()).Returns(call => compliance.Release(call.Arg<IEnumerable<AppendedEvent>>(), call.Arg<IDictionary<EventType, EventTypeSchema>>()));
        _eventGenerationRelease.Release(Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>(), Arg.Any<IEnumerable<AppendedEvent>>()).Returns(call => release.Release(call.Arg<EventStoreName>(), call.Arg<IEnumerable<EventType>>(), call.Arg<IDictionary<EventType, EventTypeSchema>>(), call.Arg<IEnumerable<AppendedEvent>>()));
        _appended = new(EventContext.Empty with { EventType = new(_pin.Id, 1), SequenceNumber = 42UL, Subject = "person", AppendedGeneration = 1 }, converter.ToExpandoObject(new JsonObject { ["name"] = "ciphertext" }, firstSchema))
        {
            OriginalContent = "{\"name\":\"ciphertext\"}",
            GenerationalContent = new Dictionary<int, string> { [1] = "{\"name\":\"ciphertext\"}", [2] = "{\"fullName\":\"ciphertext\"}" },
            GenerationalHashes = new Dictionary<int, EventHash> { [2] = "pinned-hash" }
        };
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(call =>
        {
            _delivered = call.Arg<IEnumerable<AppendedEvent>>().Single();
            return ObserverSubscriberResult.Ok(42UL);
        });
        _observer.SetSubscription(new(_observerId, _observerKey, [_pin], typeof(IObserverSubscriber), SiloAddress.Zero));
        await _observer.TransitionTo<Routing>();
        await _observer.SubscribeWithGenerationDelivery<IObserverSubscriber>(ObserverType.Reactor, [_pin], SiloAddress.Zero, EventGenerationDelivery.Pinned);
    }

    protected Task HandleEvent()
    {
        if (_includeRevision)
        {
            _appended = _appended with
            {
                RevisedGeneration = _pin.Generation,
                Revisions = [new(_pin.Generation, CorrelationId.NotSet, [], Concepts.Identities.Identity.System, DateTimeOffset.UtcNow, "{\"fullName\":\"ciphertext\"}")]
            };
        }

        return _observer.Handle("person", [_appended]);
    }

    protected static string Value(string content, string property) => JsonNode.Parse(content)![property]!.GetValue<string>();

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
