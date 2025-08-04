// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Cratis.Reflection;
using Cratis.Types;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Cratis.Chronicle.Grains.EventTypes.Kernel;

/// <summary>
/// Represents an implementation of <see cref="IEventTypes"/> for managing kernel event types.
/// </summary>
/// <param name="types"><see cref="ITypes"/> for discovering event types.</param>
/// <param name="storage"><see cref="IStorage"/> for working with underlying storage.</param>
[Singleton]
public class EventTypes(
    ITypes types,
    IStorage storage) : IEventTypes
{
    readonly JsonSchemaGenerator _jsonSchemaGenerator = new(new SystemTextJsonSchemaGeneratorSettings()
    {
        AllowReferencesWithProperties = true,
    });
    readonly Dictionary<Type, JsonSchema> _schemaByType = new();

    /// <inheritdoc/>
    public JsonSchema GetJsonSchema(Type eventType) => _schemaByType[eventType];

    /// <inheritdoc/>
    public async Task DiscoverAndRegister()
    {
        var eventTypes = types.All
            .Where(t => t.HasAttribute<EventTypeAttribute>())
            .ToArray();
        var eventStores = await storage.GetEventStores();
        foreach (var eventStore in eventStores)
        {
            foreach (var eventType in eventTypes)
            {
                var schema = _jsonSchemaGenerator.Generate(eventType);
                _schemaByType[eventType] = schema;
                await storage.GetEventStore(eventStore).EventTypes.Register(eventType.GetEventType(), schema);
            }
        }
    }
}
