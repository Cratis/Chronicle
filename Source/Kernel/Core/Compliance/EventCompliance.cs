// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// Represents an implementation of <see cref="IEventCompliance"/> for applying compliance release to event content.
/// </summary>
/// <param name="complianceManager">The <see cref="IJsonComplianceManager"/> to use for releasing compliance.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting event content.</param>
public class EventCompliance(
    IJsonComplianceManager complianceManager,
    IExpandoObjectConverter expandoObjectConverter) : IEventCompliance
{
    /// <inheritdoc/>
    public async Task<AppendedEvent> ReleaseEventContent(AppendedEvent @event, JsonSchema schema)
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

    /// <inheritdoc/>
    public async Task<AppendedEvent[]> DecryptEvents(
        IEnumerable<AppendedEvent> events,
        IDictionary<EventType, EventTypeSchema> eventTypeSchemas)
    {
        var releasedEvents = new List<AppendedEvent>();
        foreach (var @event in events)
        {
            if (!eventTypeSchemas.TryGetValue(@event.Context.EventType, out var schema) ||
                @event.Context.Subject is null ||
                !schema.Schema.HasComplianceMetadata())
            {
                releasedEvents.Add(@event);
                continue;
            }

            var released = await ReleaseEventContent(@event, schema.Schema);
            releasedEvents.Add(released);
        }

        return releasedEvents.ToArray();
    }
}
