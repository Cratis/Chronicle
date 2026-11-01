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
/// Materializes the immutable catalog only when a schema is read or changed, into store-local production storage.
/// </summary>
internal sealed class SeededEventTypesStorage : IEventTypesStorage, IDisposable
{
    readonly Cratis.Chronicle.Storage.InMemory.Events.EventTypes.EventTypesStorage _storage = new();
    readonly object _sync = new();
    readonly HashSet<EventTypeId> _materialized = [];
    EventTypeSeed? _seed;

    /// <inheritdoc/>
    public Task<bool> Register(EventType type, JsonSchema schema, EventTypeOwner owner = EventTypeOwner.Client, EventTypeSource source = EventTypeSource.Code, EventTypeVisibility visibility = EventTypeVisibility.Unspecified, string origin = "")
    {
        Materialize([type.Id]);
        return _storage.Register(type, schema, owner, source, visibility, origin);
    }

    /// <inheritdoc/>
    public Task<bool> Register(EventTypeDefinition definition)
    {
        Materialize([definition.Id]);
        return _storage.Register(definition);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeId>> Register(IEnumerable<EventTypeToRegister> eventTypes)
    {
        var registrations = eventTypes.ToArray();
        Materialize(registrations.Select(item => item.Definition.Id));
        return _storage.Register(registrations);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<KernelEventTypes::EventTypeSchema>> GetLatestForAllEventTypes()
    {
        MaterializeAll();
        return _storage.GetLatestForAllEventTypes();
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<KernelEventTypes::EventTypeSchema>> ObserveLatestForAllEventTypes()
    {
        MaterializeAll();
        return _storage.ObserveLatestForAllEventTypes();
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeDefinition>> GetAllDefinitions()
    {
        MaterializeAll();
        return _storage.GetAllDefinitions();
    }

    /// <inheritdoc/>
    public Task<EventTypeDefinition> GetDefinition(EventTypeId eventTypeId)
    {
        Materialize([eventTypeId]);
        return _storage.GetDefinition(eventTypeId);
    }

    /// <inheritdoc/>
    public Task RecordMigrationsVersion(EventTypeId eventTypeId, EventTypeMigrationsVersion version, IEnumerable<EventTypeMigrationDefinition> migrations)
    {
        Materialize([eventTypeId]);
        return _storage.RecordMigrationsVersion(eventTypeId, version, migrations);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyDictionary<EventTypeMigrationsVersion, IEnumerable<EventTypeMigrationDefinition>>> GetMigrationsVersions(EventTypeId eventTypeId)
    {
        Materialize([eventTypeId]);
        return _storage.GetMigrationsVersions(eventTypeId);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<KernelEventTypes::EventTypeSchema>> GetAllGenerationsForEventType(EventType eventType)
    {
        Materialize([eventType.Id]);
        return _storage.GetAllGenerationsForEventType(eventType);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<KernelEventTypes::EventTypeSchema>> GetFor(IEnumerable<EventTypeId> eventTypeIds)
    {
        var ids = eventTypeIds.ToArray();
        Materialize(ids);
        return _storage.GetFor(ids);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<KernelEventTypes::EventTypeSchema>> GetFor(IEnumerable<EventType> eventTypes)
    {
        var types = eventTypes.ToArray();
        Materialize(types.Select(type => type.Id));
        return _storage.GetFor(types);
    }

    /// <inheritdoc/>
    public Task<bool> HasFor(EventTypeId type, EventTypeGeneration? generation = default) =>
        _seed?.Has(type, generation) == true ? Task.FromResult(true) : _storage.HasFor(type, generation);

    /// <inheritdoc/>
    public Task<KernelEventTypes::EventTypeSchema> GetFor(EventTypeId type, EventTypeGeneration? generation = default)
    {
        Materialize([type]);
        return _storage.GetFor(type, generation);
    }

    /// <inheritdoc/>
    public void Invalidate(EventTypeId eventTypeId) => _storage.Invalidate(eventTypeId);

    /// <inheritdoc/>
    public void Dispose() => _storage.Dispose();

    /// <summary>
    /// Attaches the catalog without copying or registering its definitions.
    /// </summary>
    /// <param name="seed">The immutable seed.</param>
    internal void Seed(EventTypeSeed seed) => _seed = seed;

    void MaterializeAll() => Materialize(_seed?.Ids ?? []);

    void Materialize(IEnumerable<EventTypeId> ids)
    {
        lock (_sync)
        {
            if (_seed is null) return;
            var missing = ids.Distinct().Where(id => !_materialized.Contains(id) && _seed.Has(id)).ToArray();
            if (missing.Length == 0) return;
            var definitions = missing.Select(_seed.Materialize).ToArray();
            _storage.Register(definitions).GetAwaiter().GetResult();
            foreach (var definition in definitions)
            {
                var migrations = definition.Definition.Migrations;
                _storage.RecordMigrationsVersion(definition.Definition.Id, EventTypeMigrationsVersion.For(migrations), migrations).GetAwaiter().GetResult();
                _materialized.Add(definition.Definition.Id);
            }
        }
    }
}
