// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Selects and releases the generation pinned by an observer without persisting delivery-time migrations.
/// </summary>
/// <param name="storage">The event type storage.</param>
/// <param name="eventCompliance">The compliance release boundary.</param>
/// <param name="migrations">The plaintext migration engine.</param>
/// <param name="metadataManager">The strict compliance release boundary.</param>
/// <param name="converter">The schema-guided content converter.</param>
/// <param name="schemaCache">The effective protection schema cache.</param>
public class EventGenerationRelease(
    IStorage storage,
    IEventCompliance eventCompliance,
    IEventTypeMigrations migrations,
    IJsonSchemaMetadataManager metadataManager,
    IExpandoObjectConverter converter,
    IEventTypeSchemaCache schemaCache) : IEventGenerationRelease
{
    /// <inheritdoc/>
    public async Task<AppendedEvent[]> Release(EventStoreName eventStore, IEnumerable<EventType> pins, IDictionary<EventType, EventTypeSchema> schemas, IEnumerable<AppendedEvent> events)
    {
        var pinsById = pins.Distinct().ToDictionary(_ => _.Id);
        var definitions = new Dictionary<EventTypeId, EventTypeDefinition>();
        var eventTypes = storage.GetEventStore(eventStore).EventTypes;
        var protectedMigrations = new ProtectedEventTypeMigrations(eventTypes, migrations, metadataManager, converter);
        var releasedEvents = new List<AppendedEvent>();
        foreach (var @event in events)
        {
            if (@event.Context.EventType.Id == GlobalEventTypes.Redaction || !pinsById.TryGetValue(@event.Context.EventType.Id, out var pin))
            {
                releasedEvents.AddRange(await eventCompliance.Release([@event], schemas));
                continue;
            }

            if (!definitions.TryGetValue(pin.Id, out var definition))
            {
                definition = await eventTypes.GetDefinition(pin.Id);
                definitions[pin.Id] = definition;
            }

            if (!definition.Generations.Any(_ => _.Generation == pin.Generation) || !schemas.TryGetValue(pin, out var schema))
            {
                throw new PinnedEventTypeGenerationUnavailable(pin);
            }

            using var releaseScope = ReleaseKeys.BeginScope();
            await eventTypes.EnsureSchemasFor(schemas, [@event]);
            var released = await ReleasePinned(@event, eventStore, pin, definition, schema.Schema, protectedMigrations);
            released = await eventCompliance.ReleaseHistory(released, schemas);
            releasedEvents.Add(await ReleaseKeys.Complete(() => released));
        }

        return releasedEvents.ToArray();
    }

    static (string Content, EventHash Hash) GetRevisionContent(AppendedEvent @event, EventType revisionType)
    {
        var revision = @event.Revisions.LastOrDefault();
        if (revision is not null)
        {
            if (revision.EventTypeGeneration != revisionType.Generation || string.IsNullOrEmpty(revision.Content))
            {
                throw new PinnedEventTypeGenerationUnavailable(revisionType);
            }

            return (revision.Content, EventHash.NotSet);
        }

        if (!@event.GenerationalContent.TryGetValue((int)revisionType.Generation.Value, out var content))
        {
            throw new PinnedEventTypeGenerationUnavailable(revisionType);
        }

        return (content, @event.GenerationalHashes.GetValueOrDefault((int)revisionType.Generation.Value, EventHash.NotSet));
    }

    async Task<AppendedEvent> ReleasePinned(AppendedEvent @event, EventStoreName eventStore, EventType pin, EventTypeDefinition definition, JsonSchema schema, ProtectedEventTypeMigrations protectedMigrations)
    {
        var deliverySource = GetRevisionDeliverySource(@event, definition, pin);
        if (deliverySource.Context.EventType.Generation == pin.Generation)
        {
            return await eventCompliance.Release(deliverySource with { Context = deliverySource.Context with { EventType = pin } }, schema);
        }

        if (!@event.IsRevised && @event.GenerationalContent.TryGetValue((int)pin.Generation.Value, out var storedContent))
        {
            var effective = await schemaCache.GetProtectionSchemaFor(eventStore, pin.Id, pin.Generation, schema);
            var content = converter.ToExpandoObject(JsonNode.Parse(storedContent)!.AsObject(), effective);
            var hash = @event.GenerationalHashes.GetValueOrDefault((int)pin.Generation.Value, EventHash.NotSet);
            var selected = @event with { Content = content, Context = @event.Context with { EventType = pin, Hash = hash } };
            return await eventCompliance.Release(selected, schema);
        }

        return await ReleaseMigrated(@event, deliverySource, eventStore, pin, definition, protectedMigrations);
    }

    async Task<AppendedEvent> ReleaseMigrated(AppendedEvent @event, AppendedEvent deliverySource, EventStoreName eventStore, EventType pin, EventTypeDefinition definition, ProtectedEventTypeMigrations protectedMigrations)
    {
        var source = deliverySource.Context.EventType;
        var sourceSchema = definition.Generations.SingleOrDefault(_ => _.Generation == source.Generation)?.Schema;
        JsonObject protectedContent;
        if (!@event.IsRevised && @event.GenerationalContent.Count > 0)
        {
            var sourceGeneration = @event.Context.AppendedGeneration is { } appended && @event.GenerationalContent.ContainsKey((int)appended.Value)
                ? (int)appended.Value
                : @event.GenerationalContent.Keys.Max();
            source = new(pin.Id, (uint)sourceGeneration);
            protectedContent = JsonNode.Parse(@event.GenerationalContent[sourceGeneration])!.AsObject();
        }
        else
        {
            if (sourceSchema is null)
            {
                throw new PinnedEventTypeGenerationUnavailable(source);
            }

            protectedContent = converter.ToJsonObject(deliverySource.Content, sourceSchema);
        }

        if (!definition.Generations.Any(_ => _.Generation == source.Generation))
        {
            throw new PinnedEventTypeGenerationUnavailable(source);
        }

        var migrated = await protectedMigrations.ReleaseAndMigrateTo(eventStore, @event.Context.Namespace, definition, source, protectedContent, @event.Context.Subject.Value, pin.Generation);
        return @event with { Content = migrated, Context = @event.Context with { EventType = pin, Hash = EventHash.NotSet } };
    }

    AppendedEvent GetRevisionDeliverySource(AppendedEvent @event, EventTypeDefinition definition, EventType pin)
    {
        var revisionGeneration = @event.RevisedGeneration ?? @event.Revisions.LastOrDefault()?.EventTypeGeneration;
        if (revisionGeneration is null || revisionGeneration == @event.Context.EventType.Generation)
        {
            return @event;
        }

        var revisionType = new EventType(pin.Id, revisionGeneration);
        var revisionSchema = definition.Generations.SingleOrDefault(_ => _.Generation == revisionGeneration)?.Schema
            ?? throw new PinnedEventTypeGenerationUnavailable(revisionType);
        var (content, hash) = GetRevisionContent(@event, revisionType);
        return @event with
        {
            Content = converter.ToExpandoObject(JsonNode.Parse(content)!.AsObject(), revisionSchema),
            Context = @event.Context with { EventType = revisionType, Hash = hash }
        };
    }
}
