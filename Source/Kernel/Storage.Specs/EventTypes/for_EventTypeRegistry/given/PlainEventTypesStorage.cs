// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.EventTypes.for_EventTypeRegistry.given;

/// <summary>
/// A plain event type storage that keeps nothing but what was registered and counts how often it is read, standing in
/// for a provider. Its unknown-type behavior is the one of the MongoDB provider: it throws.
/// </summary>
public class PlainEventTypesStorage : IEventTypesStorage
{
    readonly ConcurrentDictionary<EventTypeId, EventTypeDefinition> _definitions = new();
    readonly ConcurrentDictionary<EventTypeId, (EventTypeSource Source, EventTypeVisibility Visibility)> _metadata = new();

    int _reads;

    /// <summary>
    /// Gets the number of reads storage has served.
    /// </summary>
    public int Reads => Volatile.Read(ref _reads);

    /// <summary>
    /// Gets or sets a gate that reads wait for when set. Used to hold a load in flight.
    /// </summary>
    public TaskCompletionSource? ReadGate { get; set; }

    public static JsonSchema SchemaFor(string description) => new() { Description = description };

    public void Seed(EventTypeId id, params (uint Generation, string Description)[] generations)
    {
        _definitions[id] = new EventTypeDefinition(
            id,
            EventTypeOwner.Client,
            false,
            [.. generations.Select(_ => new EventTypeGenerationDefinition(_.Generation, SchemaFor(_.Description)))],
            []);
        _metadata[id] = (EventTypeSource.Code, EventTypeVisibility.Unspecified);
    }

    public void SeedTombstone(EventTypeId id, uint generation, string description)
    {
        _definitions[id] = new EventTypeDefinition(id, EventTypeOwner.Client, true, [new EventTypeGenerationDefinition(generation, SchemaFor(description))], []);
        _metadata[id] = (EventTypeSource.Code, EventTypeVisibility.Unspecified);
    }

    public async Task<IEnumerable<EventTypeDefinition>> GetAllDefinitions()
    {
        await Read();
        return [.. _definitions.Values];
    }

    public async Task<IEnumerable<EventTypeSchema>> GetLatestForAllEventTypes()
    {
        await Read();
        return [.. _definitions.Values.Select(Latest)];
    }

    public async Task<EventTypeDefinition> GetDefinition(EventTypeId eventTypeId)
    {
        await Read();
        return _definitions.TryGetValue(eventTypeId, out var definition)
            ? definition
            : throw new MissingEventSchemaForEventType("store", eventTypeId, EventTypeGeneration.First);
    }

    public async Task<EventTypeSchema> GetFor(EventTypeId type, EventTypeGeneration? generation = default)
    {
        await Read();
        generation ??= EventTypeGeneration.First;
        return _definitions.TryGetValue(type, out var definition) && definition.Generations.FirstOrDefault(_ => _.Generation == generation) is { } found
            ? ToSchema(definition, found)
            : throw new MissingEventSchemaForEventType("store", type, generation);
    }

    public async Task<bool> HasFor(EventTypeId type, EventTypeGeneration? generation = default)
    {
        await Read();
        generation ??= EventTypeGeneration.First;
        return _definitions.TryGetValue(type, out var definition) && definition.Generations.Any(_ => _.Generation == generation);
    }

    public async Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<EventTypeId> eventTypeIds)
    {
        await Read();
        return [.. eventTypeIds.Where(_definitions.ContainsKey).Select(id => ToSchema(_definitions[id], _definitions[id].Generations.First()))];
    }

    public async Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<EventType> eventTypes)
    {
        await Read();
        return [.. eventTypes
            .Where(_ => _definitions.TryGetValue(_.Id, out var definition) && definition.Generations.Any(generation => generation.Generation == _.Generation))
            .Select(_ => ToSchema(_definitions[_.Id], _definitions[_.Id].Generations.First(generation => generation.Generation == _.Generation)))];
    }

    public Task<bool> Register(EventType type, JsonSchema schema, EventTypeOwner owner = EventTypeOwner.Client, EventTypeSource source = EventTypeSource.Code, EventTypeVisibility visibility = EventTypeVisibility.Unspecified, string origin = "")
    {
        var existing = _definitions.TryGetValue(type.Id, out var definition) ? definition : null;
        var generations = (existing?.Generations ?? []).Where(_ => _.Generation != type.Generation).Append(new EventTypeGenerationDefinition(type.Generation, schema)).ToList();
        var changed = existing is null ||
            existing.Tombstone != type.Tombstone ||
            existing.Generations.FirstOrDefault(_ => _.Generation == type.Generation)?.Schema.ToJson() != schema.ToJson();
        _definitions[type.Id] = new EventTypeDefinition(type.Id, owner, type.Tombstone, generations, existing?.Migrations ?? []);
        _metadata[type.Id] = (source, visibility);
        return Task.FromResult(changed);
    }

    public Task<bool> Register(EventTypeDefinition definition)
    {
        var changed = !_definitions.TryGetValue(definition.Id, out var existing) || existing.Generations.Count() != definition.Generations.Count();
        _definitions[definition.Id] = definition;
        _metadata.TryAdd(definition.Id, (EventTypeSource.Code, EventTypeVisibility.Unspecified));
        return Task.FromResult(changed);
    }

    public Task<bool> ReplaceSchema(EventTypeId eventTypeId, EventTypeGeneration generation, JsonSchema schema) => Task.FromResult(false);

    public ISubject<IEnumerable<EventTypeSchema>> ObserveLatestForAllEventTypes() => new Subject<IEnumerable<EventTypeSchema>>();

    public Task<IEnumerable<EventTypeSchema>> GetAllGenerationsForEventType(EventType eventType) =>
        Task.FromResult<IEnumerable<EventTypeSchema>>(_definitions.TryGetValue(eventType.Id, out var definition)
            ? [.. definition.Generations.Select(_ => ToSchema(definition, _))]
            : []);

    async Task Read()
    {
        Interlocked.Increment(ref _reads);
        if (ReadGate is { } gate)
        {
            await gate.Task;
        }
    }

    EventTypeSchema Latest(EventTypeDefinition definition) => ToSchema(definition, definition.Generations.MaxBy(_ => _.Generation.Value)!);

    EventTypeSchema ToSchema(EventTypeDefinition definition, EventTypeGenerationDefinition generation)
    {
        var (source, visibility) = _metadata[definition.Id];
        return new(new EventType(definition.Id, generation.Generation, definition.Tombstone), definition.Owner, source, generation.Schema, visibility);
    }
}
