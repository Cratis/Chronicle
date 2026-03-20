// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Concepts;
using Cratis.DependencyInjection;
using Cratis.Serialization;
using Cratis.Types;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents an implementation of <see cref="IEventTypes"/> for managing kernel event types.
/// </summary>
[Singleton]
public class EventTypes : IEventTypes
{
    readonly JsonSerializerOptions _serializerOptions;
    readonly JsonSchemaExporterOptions _exporterOptions;
    readonly Dictionary<Type, JsonSchema> _schemaByType = new();
    readonly Dictionary<EventTypeId, Type> _typeByEventTypeId = new();
    readonly ITypes _types;
    readonly IStorage _storage;
    readonly ILogger<EventTypes> _logger;
    readonly ITypeFormats _typeFormats = new TypeFormats();

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypes"/> class.
    /// </summary>
    /// <param name="types"><see cref="ITypes"/> for discovering event types.</param>
    /// <param name="storage"><see cref="IStorage"/> for working with underlying storage.</param>
    /// <param name="logger"><see cref="ILogger{TCategoryName}"/> for logging.</param>
    public EventTypes(
        ITypes types,
        IStorage storage,
        ILogger<EventTypes> logger)
    {
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new EnumerableConceptAsJsonConverterFactory(),
                new ConceptAsJsonConverterFactory()
            }
        };

        _exporterOptions = new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = TransformNode
        };

        _types = types;
        _storage = storage;
        _logger = logger;
    }

    /// <inheritdoc/>
    public JsonSchema GetJsonSchema(Type eventType) => _schemaByType[eventType];

    /// <inheritdoc/>
    public Type GetClrTypeFor(EventTypeId eventTypeId) => _typeByEventTypeId[eventTypeId];

    /// <inheritdoc/>
    public async Task DiscoverAndRegister(EventStoreName eventStore)
    {
        var eventTypes = _types.All
            .Where(t => t.IsEventType())
            .ToArray();

        var isSystemEventStore = eventStore == EventStoreName.System;
        _logger.DiscoveringAndRegistering(eventStore, eventTypes.Length);

        foreach (var eventType in eventTypes)
        {
            var isForAllEventStores = eventType.IsForAllEventStores();

            // Skip event types for the System event store that are for all event stores
            if (isSystemEventStore && isForAllEventStores)
            {
                continue;
            }

            // Skip system-only event types for non-System event stores
            if (!isSystemEventStore && !isForAllEventStores)
            {
                continue;
            }

            var schemaNode = JsonSchemaExporter.GetJsonSchemaAsNode(_serializerOptions, eventType, _exporterOptions);
            var schema = new JsonSchema(schemaNode.AsObject());
            _schemaByType[eventType] = schema;
            _typeByEventTypeId[eventType.GetEventType().Id] = eventType;
            await _storage.GetEventStore(eventStore).EventTypes.Register(eventType.GetEventType(), schema, EventTypeOwner.Server, EventTypeSource.Code);
        }
    }

    JsonNode TransformNode(JsonSchemaExporterContext context, JsonNode schema)
    {
        var type = context.TypeInfo.Type;

        // Handle concept types - redirect to the underlying primitive type's schema
        if (type.IsConcept())
        {
            var underlyingType = type.GetConceptValueType();
            return JsonSchemaExporter.GetJsonSchemaAsNode(context.TypeInfo.Options, underlyingType, _exporterOptions);
        }

        // Add format for known types
        if (_typeFormats.IsKnown(type) && schema is JsonObject schemaObj)
        {
            schemaObj["format"] = _typeFormats.GetFormatForType(type);
        }

        return schema;
    }
}
