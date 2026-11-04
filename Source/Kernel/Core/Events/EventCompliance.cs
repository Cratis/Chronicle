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
    public Task<AppendedEvent> Release(AppendedEvent @event, JsonSchema schema)
    {
        // Not async: a warm lookup completes synchronously and must not add a task on the delivery path.
        var lookup = schemaCache.GetProtectionSchemaFor(@event.Context.EventStore, @event.Context.EventType.Id, @event.Context.EventType.Generation, schema);
#pragma warning disable MA0042 // Result is only read after IsCompletedSuccessfully, so it never blocks
        return lookup.IsCompletedSuccessfully
            ? ReleaseWithEffective(@event, lookup.Result)
            : ReleaseAfterLookup(@event, lookup);
#pragma warning restore MA0042
    }

    /// <inheritdoc/>
    public Task<AppendedEvent> ReleaseReportingErasure(AppendedEvent @event, JsonSchema schema)
    {
        // Not async: a warm lookup completes synchronously and must not add a task on the delivery path.
        var lookup = schemaCache.GetProtectionSchemaFor(@event.Context.EventStore, @event.Context.EventType.Id, @event.Context.EventType.Generation, schema);
#pragma warning disable MA0042 // Result is only read after IsCompletedSuccessfully, so it never blocks
        return lookup.IsCompletedSuccessfully
            ? ReleaseReportingErasureResolved(@event, lookup.Result)
            : ReleaseReportingErasureAfterLookup(@event, lookup);
#pragma warning restore MA0042
    }

    /// <inheritdoc/>
    public async Task<AppendedEvent[]> ReleaseReportingErasure(
        IEnumerable<AppendedEvent> events,
        IDictionary<EventType, EventTypeSchema> eventTypeSchemas)
    {
        var releasedEvents = new List<AppendedEvent>();
        foreach (var @event in events)
        {
            if (!eventTypeSchemas.TryGetValue(@event.Context.EventType, out var schema))
            {
                releasedEvents.Add(@event with { ErasedProperties = [] });
                continue;
            }

            if (@event.Context.Subject is null)
            {
                releasedEvents.Add(@event with { ErasedProperties = [] });
                continue;
            }

            var effective = await schemaCache.GetProtectionSchemaFor(@event.Context.EventStore, @event.Context.EventType.Id, @event.Context.EventType.Generation, schema.Schema);
            if (!effective.HasSchemaMetadata())
            {
                releasedEvents.Add(@event with { ErasedProperties = [] });
                continue;
            }

            releasedEvents.Add(await ReleaseReportingErasureResolved(@event, effective));
        }

        return releasedEvents.ToArray();
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

    async Task<AppendedEvent> ReleaseReportingErasureAfterLookup(AppendedEvent @event, ValueTask<JsonSchema> lookup) =>
        await ReleaseReportingErasureResolved(@event, await lookup);

    async Task<AppendedEvent> ReleaseReportingErasureResolved(AppendedEvent @event, JsonSchema schema)
    {
        var contentAsJson = expandoObjectConverter.ToJsonObject(@event.Content, schema);
        var released = await complianceManager.ReleaseReportingErasedProperties(
            @event.Context.EventStore,
            @event.Context.Namespace,
            schema,
            @event.Context.Subject.Value,
            contentAsJson);

        return @event with
        {
            Content = expandoObjectConverter.ToExpandoObject(released.Value, schema),
            ErasedProperties = released.ErasedPaths
        };
    }

    async Task<AppendedEvent> ReleaseAfterLookup(AppendedEvent @event, ValueTask<JsonSchema> lookup) =>
        await ReleaseWithEffective(@event, await lookup);

    Task<AppendedEvent> ReleaseWithEffective(AppendedEvent @event, JsonSchema effective)
    {
        // Same rule as the batch overload: content without effective protection is returned untouched.
        return effective.HasSchemaMetadata() ? ReleaseResolved(@event, effective) : Task.FromResult(@event);
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
