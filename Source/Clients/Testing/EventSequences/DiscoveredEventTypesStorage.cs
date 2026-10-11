// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using System.Reactive.Subjects;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;
using KernelConcepts::Cratis.Chronicle.Concepts.Events;
using KernelEventTypes = KernelConcepts::Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Keeps schema lookup strict while delegating persistence and migration versions to production storage.
/// </summary>
/// <param name="storage">The production event type storage.</param>
/// <param name="eventTypes">The discovered client registry.</param>
internal sealed class DiscoveredEventTypesStorage(IEventTypesStorage storage, Func<Cratis.Chronicle.Events.IEventTypes> eventTypes) : IEventTypesStorage
{
    /// <inheritdoc/>
    public Task<bool> Register(EventType type, JsonSchema schema, EventTypeOwner owner = EventTypeOwner.Client, EventTypeSource source = EventTypeSource.Code, EventTypeVisibility visibility = EventTypeVisibility.Unspecified, string origin = "") =>
        storage.Register(type, schema, owner, source, visibility, origin);

    /// <inheritdoc/>
    public Task<bool> Register(EventTypeDefinition definition) => storage.Register(definition);

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeId>> Register(IEnumerable<EventTypeToRegister> eventTypes) => storage.Register(eventTypes);

    /// <inheritdoc/>
    public Task<IEnumerable<KernelEventTypes::EventTypeSchema>> GetLatestForAllEventTypes() => storage.GetLatestForAllEventTypes();

    /// <inheritdoc/>
    public ISubject<IEnumerable<KernelEventTypes::EventTypeSchema>> ObserveLatestForAllEventTypes() => storage.ObserveLatestForAllEventTypes();

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeDefinition>> GetAllDefinitions() => storage.GetAllDefinitions();

    /// <inheritdoc/>
    public Task<EventTypeDefinition> GetDefinition(EventTypeId eventTypeId)
    {
        RequireDiscovered(eventTypeId, null);
        return storage.GetDefinition(eventTypeId);
    }

    /// <inheritdoc/>
    public Task RecordMigrationsVersion(EventTypeId eventTypeId, EventTypeMigrationsVersion version, IEnumerable<EventTypeMigrationDefinition> migrations) =>
        storage.RecordMigrationsVersion(eventTypeId, version, migrations);

    /// <inheritdoc/>
    public Task<IReadOnlyDictionary<EventTypeMigrationsVersion, IEnumerable<EventTypeMigrationDefinition>>> GetMigrationsVersions(EventTypeId eventTypeId) =>
        storage.GetMigrationsVersions(eventTypeId);

    /// <inheritdoc/>
    public Task<IEnumerable<KernelEventTypes::EventTypeSchema>> GetAllGenerationsForEventType(EventType eventType)
    {
        RequireDiscovered(eventType.Id, eventType.Generation);
        return storage.GetAllGenerationsForEventType(eventType);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<KernelEventTypes::EventTypeSchema>> GetFor(IEnumerable<EventTypeId> eventTypeIds)
    {
        var types = eventTypeIds.ToArray();
        foreach (var type in types) RequireDiscovered(type, null);
        return storage.GetFor(types);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<KernelEventTypes::EventTypeSchema>> GetFor(IEnumerable<EventType> eventTypes)
    {
        var types = eventTypes.ToArray();
        foreach (var type in types) RequireDiscovered(type.Id, type.Generation);
        return storage.GetFor(types);
    }

    /// <inheritdoc/>
    public Task<bool> HasFor(EventTypeId type, EventTypeGeneration? generation = default) =>
        Task.FromResult(IsDiscovered(type, generation));

    /// <inheritdoc/>
    public Task<KernelEventTypes::EventTypeSchema> GetFor(EventTypeId type, EventTypeGeneration? generation = default)
    {
        RequireDiscovered(type, generation);
        return storage.GetFor(type, generation);
    }

    /// <inheritdoc/>
    public void Invalidate(EventTypeId eventTypeId) => storage.Invalidate(eventTypeId);

    bool IsDiscovered(EventTypeId id, EventTypeGeneration? generation) =>
        eventTypes().All.Any(type => type.Id.Value == id.Value && (generation is null || type.Generation.Value == generation.Value));

    void RequireDiscovered(EventTypeId id, EventTypeGeneration? generation)
    {
        if (!IsDiscovered(id, generation))
        {
            throw new EventSchemaNotDiscovered(id.Value, (generation ?? EventTypeGeneration.First).Value);
        }
    }
}
