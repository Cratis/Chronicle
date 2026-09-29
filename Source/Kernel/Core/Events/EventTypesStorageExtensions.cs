// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Extension methods for resolving the schema an event was stored with.
/// </summary>
public static class EventTypesStorageExtensions
{
    /// <summary>
    /// Make sure the schemas hold the schema of the exact event type - including its generation - of every event.
    /// </summary>
    /// <param name="eventTypes">The <see cref="IEventTypesStorage"/> to load missing schemas from.</param>
    /// <param name="schemas">The schemas resolved so far, keyed by the event type stored on the events. Missing entries are added.</param>
    /// <param name="events">The <see cref="AppendedEvent">events</see> that are about to be released.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Compliance metadata is read from the schema of the generation an event was stored at, and a stream holds events
    /// of every generation the event type has had. A schema is therefore resolved for the stored generation rather
    /// than for the latest, and only once per event type and generation - later batches find it in <paramref name="schemas"/>.
    /// </remarks>
    public static async Task EnsureSchemasFor(
        this IEventTypesStorage eventTypes,
        IDictionary<EventType, EventTypeSchema> schemas,
        IEnumerable<AppendedEvent> events)
    {
        // A redaction marker is not a registered event type and carries no compliance metadata, so it is never looked
        // up - otherwise every batch holding one would query storage again for a schema that does not exist.
        var missing = events
            .Select(_ => _.Context.EventType)
            .Where(_ => _.Id != GlobalEventTypes.Redaction)
            .Distinct()
            .Where(_ => !schemas.ContainsKey(_))
            .ToArray();

        if (missing.Length == 0)
        {
            return;
        }

        var stored = (await eventTypes.GetFor(missing)).ToArray();
        foreach (var eventType in missing)
        {
            var schema = stored.FirstOrDefault(_ => _.Type.Id == eventType.Id && _.Type.Generation == eventType.Generation);
            if (schema is not null)
            {
                schemas[eventType] = schema;
            }
        }
    }

    /// <summary>
    /// Get the schema of the exact event type - including its generation - an event was stored with.
    /// </summary>
    /// <param name="eventTypes">The <see cref="IEventTypesStorage"/> to load the schema from.</param>
    /// <param name="eventType">The <see cref="EventType"/> stored on the event.</param>
    /// <returns>The <see cref="JsonSchema"/> of that generation, or null if the event type is not stored.</returns>
    public static async Task<JsonSchema?> GetStoredSchemaFor(this IEventTypesStorage eventTypes, EventType eventType)
    {
        var stored = await eventTypes.GetFor([eventType]);
        return stored.FirstOrDefault(_ => _.Type.Id == eventType.Id && _.Type.Generation == eventType.Generation)?.Schema;
    }
}
