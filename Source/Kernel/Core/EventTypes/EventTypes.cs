// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Cratis.Strings;
using Cratis.Types;
using Microsoft.Extensions.Logging;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents an implementation of <see cref="IEventTypes"/> for managing kernel event types.
/// </summary>
[Singleton]
public class EventTypes : IEventTypes
{
    readonly JsonSchemaGenerator _jsonSchemaGenerator;
    readonly Dictionary<Type, JsonSchema> _schemaByType = new();
    readonly Dictionary<EventTypeId, Type> _typeByEventTypeId = new();
    readonly ITypes _types;
    readonly IStorage _storage;
    readonly ILogger<EventTypes> _logger;

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
        var settings = new SystemTextJsonSchemaGeneratorSettings()
        {
            AllowReferencesWithProperties = true
        };
        settings.ReflectionService = new ReflectionService(settings.ReflectionService);
        settings.SchemaProcessors.Add(new TypeFormatSchemaProcessor(new TypeFormats()));
        _jsonSchemaGenerator = new(settings);
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

            var schema = _jsonSchemaGenerator.Generate(eventType);
            ForceSchemaToBeCamelCase(schema);
            _schemaByType[eventType] = schema;
            _typeByEventTypeId[eventType.GetEventType().Id] = eventType;
            await _storage.GetEventStore(eventStore).EventTypes.Register(eventType.GetEventType(), schema, EventTypeOwner.Server, EventTypeSource.Code);
        }
    }

    void ForceSchemaToBeCamelCase(JsonSchema schema)
    {
        var properties = schema.Properties.ToDictionary(kvp => kvp.Key.ToCamelCase(), kvp => kvp.Value);
        schema.Properties.Clear();
        foreach (var kvp in properties)
        {
            schema.Properties.Add(kvp);
        }

        foreach (var property in schema.Properties.Values)
        {
            ForceSchemaToBeCamelCase(property);
        }

        if (schema.Item != null)
        {
            ForceSchemaToBeCamelCase(schema.Item);
        }

        foreach (var allOfSchema in schema.AllOf)
        {
            ForceSchemaToBeCamelCase(allOfSchema);
        }

        foreach (var anyOfSchema in schema.AnyOf)
        {
            ForceSchemaToBeCamelCase(anyOfSchema);
        }

        foreach (var oneOfSchema in schema.OneOf)
        {
            ForceSchemaToBeCamelCase(oneOfSchema);
        }

        if (schema.HasReference)
        {
            ForceSchemaToBeCamelCase(schema.Reference!);
        }

        if (schema.AdditionalPropertiesSchema != null)
        {
            ForceSchemaToBeCamelCase(schema.AdditionalPropertiesSchema);
        }

        foreach (var definition in schema.Definitions.Values)
        {
            ForceSchemaToBeCamelCase(definition);
        }
    }
}
