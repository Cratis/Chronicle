// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.given;

public class a_pinned_subscriber : a_subscriber_with_a_cached_pipeline
{
    protected EventType _pin = new("person-registered", 2);
    protected AppendedEvent _event;
    protected AppendedEvent _delivered;
    protected IEventTypeMigrations _migrations;
    protected IEventGenerationRelease _release;

    void Establish()
    {
        var converter = new ExpandoObjectConverter(Substitute.For<ITypeFormats>());
        var firstSchema = JsonSchema.FromJson("{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}");
        var pinnedSchema = JsonSchema.FromJson("{\"type\":\"object\",\"properties\":{\"fullName\":{\"type\":\"string\"}}}");
        var eventTypes = Substitute.For<IEventTypesStorage>();
        eventTypes.GetDefinition(_pin.Id).Returns(new EventTypeDefinition(_pin.Id, EventTypeOwner.Client, false, [new(1, firstSchema), new(2, pinnedSchema)], []));
        var storage = Substitute.For<Storage.IStorage>();
        storage.GetEventStore(_key.EventStore).EventTypes.Returns(eventTypes);
        var metadata = Substitute.For<IJsonSchemaMetadataManager>();
        metadata.ReleaseStrict(_key.EventStore, _key.Namespace, firstSchema, Arg.Any<string>(), Arg.Any<JsonObject>()).Returns(call => (JsonObject)call[4]);
        _migrations = Substitute.For<IEventTypeMigrations>();
        _migrations.MigrateToAllGenerations(Arg.Any<EventTypeDefinition>(), Arg.Any<EventType>(), Arg.Any<JsonObject>(), Arg.Any<ExpandoObject>())
            .Returns(call => new Dictionary<EventTypeGeneration, ExpandoObject>
            {
                [2] = converter.ToExpandoObject(new JsonObject { ["fullName"] = ((JsonObject)call[2])["name"]!.DeepClone() }, pinnedSchema)
            });
        _release = new EventGenerationRelease(storage, new EventCompliance(metadata, converter), _migrations, metadata, converter);
        var schemas = new Dictionary<EventType, EventTypeSchema> { [_pin] = new(_pin, EventTypeOwner.Client, EventTypeSource.Code, pinnedSchema) };
        _projection.GenerationDelivery.Returns(EventGenerationDelivery.Pinned);
        _projection.EventTypes.Returns([_pin]);
        _projection.Accepts(Arg.Any<EventType>()).Returns(call => (EventType)call[0] == _pin);
        _projection.ReleaseEvent(Arg.Any<EventType>(), Arg.Any<AppendedEvent>()).Returns(async call =>
            (await _release.Release(_key.EventStore, [(EventType)call[0]], schemas, [(AppendedEvent)call[1]])).Single());
        _event = new(EventContext.Empty with { EventType = new(_pin.Id, 1), EventStore = _key.EventStore, Namespace = _key.Namespace, SequenceNumber = 42, AppendedGeneration = 1 }, converter.ToExpandoObject(new JsonObject { ["name"] = "Ada Lovelace" }, firstSchema))
        {
            GenerationalContent = new Dictionary<int, string> { [1] = "{\"name\":\"Ada Lovelace\"}" }
        };
        _cachedPipeline.Handle(Arg.Any<AppendedEvent>()).Returns(call => Handle((AppendedEvent)call[0]));
        _refreshedPipeline.Handle(Arg.Any<AppendedEvent>()).Returns(call => Handle((AppendedEvent)call[0]));
    }

    ProjectionEventContext Handle(AppendedEvent @event)
    {
        _delivered = @event;
        return ProjectionEventContext.Empty(Substitute.For<IObjectComparer>(), @event);
    }
}
