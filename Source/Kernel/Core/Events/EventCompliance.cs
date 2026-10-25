// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventCompliance"/> for applying compliance release to event content.
/// </summary>
/// <param name="complianceManager">The <see cref="IJsonSchemaMetadataManager"/> to use for releasing compliance.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting event content.</param>
public class EventCompliance(
    IJsonSchemaMetadataManager complianceManager,
    IExpandoObjectConverter expandoObjectConverter) : IEventCompliance
{
    /// <inheritdoc/>
    public async Task<AppendedEvent> Release(AppendedEvent @event, JsonSchema schema)
    {
        using var scope = ReleaseKeys.BeginScope();
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
    public async Task<AppendedEvent> ReleaseHistory(AppendedEvent @event, IDictionary<EventType, EventTypeSchema> eventTypeSchemas)
    {
        using var scope = ReleaseKeys.BeginScope();
        if (@event.Context.Subject is null) return @event;

        var generations = new Dictionary<int, string>();
        foreach (var (generation, content) in @event.GenerationalContent)
        {
            generations[generation] = await ReleaseHistoryContent(@event, (uint)generation, content, eventTypeSchemas);
        }

        var revisions = new List<EventRevision>();
        foreach (var revision in @event.Revisions)
        {
            revisions.Add(revision with { Content = await ReleaseHistoryContent(@event, revision.EventTypeGeneration, revision.Content, eventTypeSchemas) });
        }

        var originalContent = await ReleaseHistoryContent(@event, EventTypeGeneration.First, @event.OriginalContent, eventTypeSchemas);
        var unchanged = originalContent == @event.OriginalContent && generations.SequenceEqual(@event.GenerationalContent) && revisions.SequenceEqual(@event.Revisions);
        if (unchanged) return @event;

        return @event with
        {
            GenerationalContent = generations,
            OriginalContent = originalContent,
            Revisions = revisions
        };
    }

    /// <inheritdoc/>
    public async Task<AppendedEvent[]> Release(
        IEnumerable<AppendedEvent> events,
        IDictionary<EventType, EventTypeSchema> eventTypeSchemas)
    {
        using var scope = ReleaseKeys.BeginScope();
        var releasedEvents = new List<AppendedEvent>();
        foreach (var @event in events)
        {
            var released = @event;
            if (@event.Context.Subject is not null &&
                eventTypeSchemas.TryGetValue(@event.Context.EventType, out var schema) &&
                schema.Schema.HasSchemaMetadata())
            {
                released = await Release(@event, schema.Schema);
            }

            releasedEvents.Add(await ReleaseHistory(released, eventTypeSchemas));
        }

        return releasedEvents.ToArray();
    }

    async Task<string> ReleaseHistoryContent(AppendedEvent @event, EventTypeGeneration generation, string content, IDictionary<EventType, EventTypeSchema> schemas)
    {
        var eventType = new EventType(@event.Context.EventType.Id, generation);
        if (string.IsNullOrEmpty(content) || !schemas.TryGetValue(eventType, out var schema) || !schema.Schema.HasSchemaMetadata()) return content;

        var released = await complianceManager.Release(
            @event.Context.EventStore,
            @event.Context.Namespace,
            schema.Schema,
            @event.Context.Subject.Value,
            JsonNode.Parse(content)!.AsObject());
        return released.ToJsonString();
    }
}
