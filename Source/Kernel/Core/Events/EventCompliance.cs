// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
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
    public Task<AppendedEvent[]> ReleaseReportingErasure(
        IEnumerable<AppendedEvent> events,
        IDictionary<EventType, EventTypeSchema> eventTypeSchemas) => ReleaseBatch(events, eventTypeSchemas, reportErasure: true);

    /// <inheritdoc/>
    public Task<AppendedEvent[]> Release(
        IEnumerable<AppendedEvent> events,
        IDictionary<EventType, EventTypeSchema> eventTypeSchemas) => ReleaseBatch(events, eventTypeSchemas, reportErasure: false);

    /// <inheritdoc/>
    public async Task<AppendedEvent> ReleaseHistory(AppendedEvent @event, IDictionary<EventType, EventTypeSchema> eventTypeSchemas)
    {
        using var releaseScope = ReleaseKeys.BeginScope();
        if (@event.Context.EventType.Id == GlobalEventTypes.Redaction || @event.Context.Subject is null || (@event.GenerationalContent.Count == 0 && @event.OriginalContent.Length == 0 && !@event.Revisions.Any()))
        {
            return @event;
        }

        var generations = new Dictionary<int, JsonObject?>();
        foreach (var (generation, content) in @event.GenerationalContent)
        {
            generations[generation] = await ReleaseHistoryContent(@event, new(@event.Context.EventType.Id, (uint)generation), content, eventTypeSchemas);
        }

        var original = await ReleaseHistoryContent(@event, new(@event.Context.EventType.Id, EventTypeGeneration.First), @event.OriginalContent, eventTypeSchemas);
        var revisions = new List<(EventRevision Revision, JsonObject? Content)>();
        foreach (var revision in @event.Revisions)
        {
            revisions.Add((revision, await ReleaseHistoryContent(@event, new(@event.Context.EventType.Id, revision.EventTypeGeneration), revision.Content, eventTypeSchemas)));
        }

        // Serialize only at the lease-validated hand-off, after protected values have settled.
        return await ReleaseKeys.Complete(() => @event with
        {
            GenerationalContent = @event.GenerationalContent.ToDictionary(item => item.Key, item => generations[item.Key]?.ToJsonString() ?? item.Value),
            OriginalContent = original?.ToJsonString() ?? @event.OriginalContent,
            Revisions = revisions.Select(item => item.Revision with { Content = item.Content?.ToJsonString() ?? item.Revision.Content }).ToArray()
        });
    }

    static bool TryReadObject(string content, out JsonObject? json)
    {
        try
        {
            json = JsonNode.Parse(content) as JsonObject;
            return json is not null;
        }
        catch (JsonException)
        {
            json = null;
            return false;
        }
    }

    async Task<AppendedEvent[]> ReleaseBatch(IEnumerable<AppendedEvent> events, IDictionary<EventType, EventTypeSchema> eventTypeSchemas, bool reportErasure)
    {
        using var releaseScope = ReleaseKeys.BeginScope();
        var releasedEvents = new List<AppendedEvent>();
        foreach (var @event in events)
        {
            var released = reportErasure ? @event with { ErasedProperties = [] } : @event;
            if (eventTypeSchemas.TryGetValue(@event.Context.EventType, out var schema) && @event.Context.Subject is not null)
            {
                var effective = await schemaCache.GetProtectionSchemaFor(@event.Context.EventStore, @event.Context.EventType.Id, @event.Context.EventType.Generation, schema.Schema);
                if (effective.HasSchemaMetadata())
                {
                    released = reportErasure ? await ReleaseReportingErasureResolved(@event, effective) : await ReleaseResolved(@event, effective);
                }
            }

            releasedEvents.Add(await ReleaseHistory(released, eventTypeSchemas));
        }

        return await ReleaseKeys.Complete(releasedEvents.ToArray);
    }

    async Task<JsonObject?> ReleaseHistoryContent(AppendedEvent @event, EventType eventType, string content, IDictionary<EventType, EventTypeSchema> eventTypeSchemas)
    {
        if (string.IsNullOrEmpty(content) || !eventTypeSchemas.TryGetValue(eventType, out var schema))
        {
            return null;
        }

        var effective = await schemaCache.GetProtectionSchemaFor(@event.Context.EventStore, eventType.Id, eventType.Generation, schema.Schema);
        if (!effective.HasSchemaMetadata() || !TryReadObject(content, out var json))
        {
            return null;
        }

        return await complianceManager.Release(@event.Context.EventStore, @event.Context.Namespace, effective, @event.Context.Subject.Value, json!);
    }

    async Task<AppendedEvent> ReleaseReportingErasureAfterLookup(AppendedEvent @event, ValueTask<JsonSchema> lookup) =>
        await ReleaseReportingErasureResolved(@event, await lookup);

    async Task<AppendedEvent> ReleaseReportingErasureResolved(AppendedEvent @event, JsonSchema schema)
    {
        using var releaseScope = ReleaseKeys.BeginScope();
        var contentAsJson = expandoObjectConverter.ToJsonObject(@event.Content, schema);
        var released = await complianceManager.ReleaseReportingErasedProperties(
            @event.Context.EventStore,
            @event.Context.Namespace,
            schema,
            @event.Context.Subject.Value,
            contentAsJson);

        return await ReleaseKeys.Complete(() => @event with
        {
            Content = expandoObjectConverter.ToExpandoObject(released.Value, schema),
            ErasedProperties = released.ErasedPaths
        });
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
        using var releaseScope = ReleaseKeys.BeginScope();
        var contentAsJson = expandoObjectConverter.ToJsonObject(@event.Content, schema);
        var released = await complianceManager.Release(
            @event.Context.EventStore,
            @event.Context.Namespace,
            schema,
            @event.Context.Subject.Value,
            contentAsJson);

        var releasedContent = expandoObjectConverter.ToExpandoObject(released, schema);
        return await ReleaseKeys.Complete(() => @event with { Content = releasedContent });
    }
}
