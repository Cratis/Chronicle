// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// Helper methods for applying compliance release to event content.
/// </summary>
public static class EventComplianceHelper
{
    /// <summary>
    /// Decrypts PII fields in an appended event content payload for a given schema.
    /// </summary>
    /// <param name="complianceManager">The <see cref="IJsonComplianceManager"/> to use for releasing compliance.</param>
    /// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting event content.</param>
    /// <param name="event">The <see cref="AppendedEvent"/> to decrypt.</param>
    /// <param name="schema">The <see cref="JsonSchema"/> describing the event payload.</param>
    /// <returns>The event with decrypted content.</returns>
    public static async Task<AppendedEvent> ReleaseEventContent(
        IJsonComplianceManager complianceManager,
        IExpandoObjectConverter expandoObjectConverter,
        AppendedEvent @event,
        JsonSchema schema)
    {
        var contentAsJson = expandoObjectConverter.ToJsonObject(@event.Content, schema);
        var released = await complianceManager.Release(
            @event.Context.EventStore,
            @event.Context.Namespace,
            schema,
            @event.Context.Subject.Value,
            contentAsJson);

        var releasedContent = expandoObjectConverter.ToExpandoObject(released, schema);
        return @event with { Content = releasedContent };
    }

    /// <summary>
    /// Decrypts PII fields for a collection of events, skipping events whose type is not in the schema map
    /// or whose subject is null.
    /// </summary>
    /// <param name="complianceManager">The <see cref="IJsonComplianceManager"/> to use for releasing compliance.</param>
    /// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting event content.</param>
    /// <param name="events">The events to decrypt.</param>
    /// <param name="eventTypeSchemas">Lookup of <see cref="EventTypeSchema"/> keyed by <see cref="EventType"/>.</param>
    /// <returns>Array of events with PII content decrypted where applicable.</returns>
    public static async Task<AppendedEvent[]> DecryptEvents(
        IJsonComplianceManager complianceManager,
        IExpandoObjectConverter expandoObjectConverter,
        IEnumerable<AppendedEvent> events,
        IDictionary<EventType, EventTypeSchema> eventTypeSchemas)
    {
        var releasedEvents = new List<AppendedEvent>();
        foreach (var @event in events)
        {
            if (!eventTypeSchemas.TryGetValue(@event.Context.EventType, out var schema) ||
                @event.Context.Subject is null)
            {
                releasedEvents.Add(@event);
                continue;
            }

            var released = await ReleaseEventContent(complianceManager, expandoObjectConverter, @event, schema.Schema);
            releasedEvents.Add(released);
        }

        return releasedEvents.ToArray();
    }
}
