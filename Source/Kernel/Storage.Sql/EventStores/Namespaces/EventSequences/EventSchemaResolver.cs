// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Resolves schemas once per type and generation within a cursor or bounded page.
/// </summary>
/// <param name="storage">The event store's shared schema storage.</param>
internal sealed class EventSchemaResolver(IEventTypesStorage storage)
{
    readonly Dictionary<EventType, Task<JsonSchema?>> _schemas = [];

    /// <summary>
    /// Gets the schema without repeating storage lookups for events in the same read.
    /// </summary>
    /// <param name="type">The event type and generation.</param>
    /// <returns>The schema, or null for an unknown generation.</returns>
    internal Task<JsonSchema?> GetFor(EventType type)
    {
        if (!_schemas.TryGetValue(type, out var schema))
        {
            _schemas[type] = schema = Resolve(type);
        }
        return schema;
    }

    async Task<JsonSchema?> Resolve(EventType type)
    {
        try
        {
            return (await storage.GetFor(type.Id, type.Generation))?.Schema;
        }
        catch (UnknownEventType)
        {
            return null;
        }
        catch (MissingEventSchemaForEventType)
        {
            return null;
        }
    }
}
