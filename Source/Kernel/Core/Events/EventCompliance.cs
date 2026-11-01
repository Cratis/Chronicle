// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventCompliance"/> for applying compliance release to event content.
/// </summary>
/// <param name="complianceManager">The <see cref="IJsonSchemaMetadataManager"/> to use for releasing compliance.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting event content.</param>
/// <param name="schemaCache">The effective protection schema cache.</param>
public class EventCompliance(
    IJsonSchemaMetadataManager complianceManager,
    IExpandoObjectConverter expandoObjectConverter,
    IEventTypeSchemaCache schemaCache) : IEventCompliance
{
    /// <inheritdoc/>
    public async Task<AppendedEvent> Release(AppendedEvent @event, JsonSchema schema)
    {
        var effective = await schemaCache.GetProtectionSchemaFor(@event.Context.EventStore, @event.Context.EventType.Id, @event.Context.EventType.Generation, schema);
        return await ReleaseResolved(@event, effective);
    }

    /// <inheritdoc/>
    public async Task<AppendedEvent[]> Release(
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

            var effective = await schemaCache.GetProtectionSchemaFor(@event.Context.EventStore, @event.Context.EventType.Id, @event.Context.EventType.Generation, schema.Schema);
            if (!effective.HasSchemaMetadata())
            {
                releasedEvents.Add(@event);
                continue;
            }

            var released = await ReleaseResolved(@event, effective);
            releasedEvents.Add(released);
        }

        return releasedEvents.ToArray();
    }

    async Task<AppendedEvent> ReleaseResolved(AppendedEvent @event, JsonSchema schema)
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
}
