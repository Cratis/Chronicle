// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using System.Reactive.Subjects;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;
using KernelConcepts::Cratis.Chronicle.Concepts.Events;
using ClientEventTypes = Cratis.Chronicle.Events.IEventTypes;
using KernelEventTypes = KernelConcepts::Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Resolves the discovered client schemas for the exact stored event generation.
/// </summary>
/// <remarks>
/// Registration and migrations are not simulated. Unknown generations fail rather than borrowing
/// the latest generation's compliance markings. The registry is resolved lazily during store construction.
/// </remarks>
/// <param name="eventTypes">The scenario's discovered client registry.</param>
/// <param name="schemaGenerator">The scenario's schema generator, including its compliance metadata providers.</param>
internal sealed class InMemoryEventTypesStorage(Func<ClientEventTypes> eventTypes, IJsonSchemaGenerator schemaGenerator) : IEventTypesStorage
{
    readonly Lazy<Dictionary<EventType, KernelEventTypes::EventTypeSchema>> _schemas = new(() =>
        eventTypes().All.ToDictionary(
            type => new EventType(type.Id.Value, type.Generation.Value),
            type => new KernelEventTypes::EventTypeSchema(
                new EventType(type.Id.Value, type.Generation.Value),
                EventTypeOwner.Client,
                EventTypeSource.Code,
                schemaGenerator.Generate(eventTypes().GetClrTypeFor(type.Id, type.Generation)))));

    /// <inheritdoc/>
    public Task<bool> Register(EventType type, JsonSchema schema, EventTypeOwner owner = EventTypeOwner.Client, EventTypeSource source = EventTypeSource.Code) =>
        Task.FromResult(false);

    /// <inheritdoc/>
    public Task<bool> Register(EventTypeDefinition definition) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<IEnumerable<KernelEventTypes::EventTypeSchema>> GetLatestForAllEventTypes() =>
        Task.FromResult(_schemas.Value.Values.GroupBy(schema => schema.Type.Id).Select(group => group.MaxBy(schema => schema.Type.Generation.Value)!));

    /// <inheritdoc/>
    public ISubject<IEnumerable<KernelEventTypes::EventTypeSchema>> ObserveLatestForAllEventTypes() =>
        new BehaviorSubject<IEnumerable<KernelEventTypes::EventTypeSchema>>(_schemas.Value.Values.GroupBy(schema => schema.Type.Id).Select(group => group.MaxBy(schema => schema.Type.Generation.Value)!));

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeDefinition>> GetAllDefinitions() =>
        Task.FromResult(_schemas.Value.Keys.Select(type => type.Id).Distinct().Select(DefinitionFor));

    /// <inheritdoc/>
    public Task<EventTypeDefinition> GetDefinition(EventTypeId eventTypeId) => Task.FromResult(DefinitionFor(eventTypeId));

    /// <inheritdoc/>
    public Task<IEnumerable<KernelEventTypes::EventTypeSchema>> GetAllGenerationsForEventType(EventType eventType) =>
        Task.FromResult(_schemas.Value.Values.Where(schema => schema.Type.Id == eventType.Id));

    /// <inheritdoc/>
    public Task<IEnumerable<KernelEventTypes::EventTypeSchema>> GetFor(IEnumerable<EventTypeId> eventTypeIds) =>
        Task.FromResult<IEnumerable<KernelEventTypes::EventTypeSchema>>([.. eventTypeIds.Select(id => SchemaFor(id))]);

    /// <inheritdoc/>
    public Task<IEnumerable<KernelEventTypes::EventTypeSchema>> GetFor(IEnumerable<EventType> eventTypes) =>
        Task.FromResult<IEnumerable<KernelEventTypes::EventTypeSchema>>([.. eventTypes.Select(type => SchemaFor(type.Id, type.Generation))]);

    /// <inheritdoc/>
    public Task<bool> HasFor(EventTypeId type, EventTypeGeneration? generation = default) =>
        Task.FromResult(_schemas.Value.Keys.Any(key => key.Id == type && (generation is null || key.Generation == generation)));

    /// <inheritdoc/>
    public Task<KernelEventTypes::EventTypeSchema> GetFor(EventTypeId type, EventTypeGeneration? generation = default) =>
        Task.FromResult(SchemaFor(type, generation));

    /// <inheritdoc/>
    public void Invalidate(EventTypeId eventTypeId)
    {
        // Schemas are immutable discovery snapshots for the lifetime of the scenario.
    }

    KernelEventTypes::EventTypeSchema SchemaFor(EventTypeId id, EventTypeGeneration? generation = null) =>
        _schemas.Value.TryGetValue(new EventType(id, generation ?? EventTypeGeneration.First), out var schema)
            ? schema
            : throw new EventSchemaNotDiscovered(id.Value, (generation ?? EventTypeGeneration.First).Value);

    EventTypeDefinition DefinitionFor(EventTypeId id)
    {
        if (!_schemas.Value.Keys.Any(type => type.Id == id)) throw new EventSchemaNotDiscovered(id.Value, EventTypeGeneration.First.Value);
        return new(
            id,
            EventTypeOwner.Client,
            false,
            [.. _schemas.Value.Values.Where(schema => schema.Type.Id == id).OrderBy(schema => schema.Type.Generation.Value)
                .Select(schema => new EventTypeGenerationDefinition(schema.Type.Generation, schema.Schema))],
            []);
    }
}
