// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.given;

public class a_release_boundary : Specification
{
    protected EventGenerationRelease _release;
    protected IStorage _storage;
    protected IEventTypesStorage _eventTypes;
    protected IEventTypeMigrations _migrations;
    protected IJsonSchemaMetadataManager _metadata;
    protected IExpandoObjectConverter _converter;
    protected EventType _pin = new("person-registered", 2);
    protected JsonSchema _firstSchema;
    protected JsonSchema _schema;
    protected EventTypeDefinition _definition;
    protected IDictionary<EventType, EventTypeSchema> _schemas;
    protected AppendedEvent _event;
    protected AppendedEvent[] _result;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _eventTypes = Substitute.For<IEventTypesStorage>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).EventTypes.Returns(_eventTypes);
        _migrations = Substitute.For<IEventTypeMigrations>();
        _metadata = Substitute.For<IJsonSchemaMetadataManager>();
        _converter = new ExpandoObjectConverter(Substitute.For<ITypeFormats>());
        _firstSchema = CreateSchema("name");
        _schema = CreateSchema("fullName");
        _definition = new(_pin.Id, EventTypeOwner.Client, false, [new(1, _firstSchema), new(2, _schema)], []);
        _eventTypes.GetDefinition(_pin.Id).Returns(_ => _definition);
        _metadata.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(call => ReleaseContent((JsonObject)call[4]));
        _metadata.ReleaseStrict(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(call => ReleaseContent((JsonObject)call[4]));
        _migrations.MigrateToAllGenerations(Arg.Any<EventTypeDefinition>(), Arg.Any<EventType>(), Arg.Any<JsonObject>(), Arg.Any<ExpandoObject>())
            .Returns(call =>
            {
                var content = (JsonObject)call[2];
                dynamic migrated = new ExpandoObject();
                migrated.fullName = content[0]!.GetValue<string>();
                return new Dictionary<EventTypeGeneration, ExpandoObject> { [2] = migrated };
            });
        _schemas = new Dictionary<EventType, EventTypeSchema> { [_pin] = new(_pin, EventTypeOwner.Client, EventTypeSource.Code, _schema) };
        _event = new(EventContext.Empty with { EventType = new(_pin.Id, 1), Subject = "person", AppendedGeneration = 1, Hash = "first-hash" }, _converter.ToExpandoObject(new JsonObject { ["name"] = "ciphertext" }, _firstSchema))
        {
            GenerationalContent = new Dictionary<int, string> { [1] = "{\"name\":\"ciphertext\"}", [2] = "{\"fullName\":\"ciphertext\"}" },
            GenerationalHashes = new Dictionary<int, EventHash> { [1] = "first-hash", [2] = "second-hash" }
        };
        _release = new(_storage, new EventCompliance(_metadata, _converter), _migrations, _metadata, _converter);
    }

    protected static JsonSchema CreateSchema(string property)
    {
        var schema = JsonSchema.FromJson($"{{\"type\":\"object\",\"properties\":{{\"{property}\":{{\"type\":\"string\"}}}}}}");
        schema.Properties[property].ExtensionData = new Dictionary<string, object?>
        {
            [ComplianceJsonSchemaExtensions.ComplianceKey] = new[] { new ComplianceSchemaMetadata("PII", string.Empty) }
        };
        return schema;
    }

    static JsonObject ReleaseContent(JsonObject content)
    {
        var released = (JsonObject)content.DeepClone();
        foreach (var property in released.ToArray())
        {
            if (property.Value?.GetValue<string>() == "ciphertext")
            {
                released[property.Key] = "Ada Lovelace";
            }
        }

        return released;
    }
}
