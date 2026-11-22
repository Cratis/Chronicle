// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.EventTypes;

/// <summary>
/// The one place event types are cached: the silo-wide, immutable registry of the event types of one event store.
/// </summary>
/// <remarks>
/// <para>
/// This is what <see cref="IEventStoreStorage.EventTypes"/> hands out, wrapped around the plain storage of the
/// provider. Everything that resolves an event type - appends, observers, projections, the serializer - reads the
/// same <see cref="EventTypeRegistrySnapshot"/>, so there is one parsed schema per event type generation per silo,
/// and no storage read once it is loaded. The storage providers keep no cache of their own.
/// </para>
/// <para>
/// Freshness is a revision race, settled like this. Every <see cref="Invalidate"/> bumps the invalidation counter. A
/// load captures the counter before it reads storage and stamps the snapshot it produces with that value, so a
/// snapshot is current only while its stamp equals the counter. An invalidation that arrives while a load is in
/// flight leaves the result stale and the next reader loads again, and a snapshot older than a just-registered type
/// is never served after the invalidation for it. A registration writes storage first and announces after, so a load
/// in between either sees the new type or is stale by the counter. <see cref="SafetyNetLifetime"/> bounds how long a
/// missed announcement can matter, and a lookup that misses reloads at most once per <see cref="MissReloadInterval"/>
/// so a type registered by a silo whose announcement has not arrived yet is still found.
/// </para>
/// <para>
/// A lookup that is still a miss after that is answered by the wrapped storage, which keeps the behavior for unknown
/// event types and generations exactly what the provider says it is. Lookups without a generation, which the providers
/// answer differently, are answered by the wrapped storage too.
/// </para>
/// </remarks>
/// <param name="inner">The plain <see cref="IEventTypesStorage"/> of the provider.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/> the lifetimes are measured with.</param>
public sealed class EventTypeRegistry(IEventTypesStorage inner, TimeProvider timeProvider) : IEventTypesStorage, IDisposable
{
    /// <summary>
    /// How long a snapshot is trusted without an announcement before it is reloaded anyway.
    /// </summary>
    public static readonly TimeSpan SafetyNetLifetime = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The least time between two reloads caused by lookups that miss.
    /// </summary>
    public static readonly TimeSpan MissReloadInterval = TimeSpan.FromSeconds(1);

    readonly SemaphoreSlim _loading = new(1, 1);
    readonly Lock _stamp = new();
    EventTypeRegistrySnapshot? _snapshot;
    long _invalidations;
    long _validatedAgainst = -1;
    DateTimeOffset _validatedAt;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypeRegistry"/> class.
    /// </summary>
    /// <param name="inner">The plain <see cref="IEventTypesStorage"/> of the provider.</param>
    public EventTypeRegistry(IEventTypesStorage inner)
        : this(inner, TimeProvider.System)
    {
    }

    /// <summary>
    /// Gets the current snapshot, loading it when there is none or it is no longer current.
    /// </summary>
    /// <returns>The current <see cref="EventTypeRegistrySnapshot"/>.</returns>
    public ValueTask<EventTypeRegistrySnapshot> GetSnapshot() => GetCurrent(false);

    /// <inheritdoc/>
    public void Dispose()
    {
        _loading.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public void Invalidate(EventTypeId eventTypeId)
    {
        lock (_stamp)
        {
            _invalidations++;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ReplaceSchema(EventTypeId eventTypeId, EventTypeGeneration generation, JsonSchema schema) =>
        InvalidateIfMutated(eventTypeId, await inner.ReplaceSchema(eventTypeId, generation, schema).ConfigureAwait(false));

    /// <inheritdoc/>
    public async Task<bool> TryReplaceSchema(EventTypeId eventTypeId, EventTypeGeneration generation, JsonSchema expected, JsonSchema schema) =>
        InvalidateIfMutated(eventTypeId, await inner.TryReplaceSchema(eventTypeId, generation, expected, schema).ConfigureAwait(false));

    /// <inheritdoc/>
    public async Task<bool> Register(EventType type, JsonSchema schema, EventTypeOwner owner = EventTypeOwner.Client, EventTypeSource source = EventTypeSource.Code, EventTypeVisibility visibility = EventTypeVisibility.Unspecified, string origin = "") =>
        InvalidateIfMutated(type.Id, await inner.Register(type, schema, owner, source, visibility, origin).ConfigureAwait(false));

    /// <inheritdoc/>
    public async Task<bool> Register(EventTypeDefinition definition) =>
        InvalidateIfMutated(definition.Id, await inner.Register(definition).ConfigureAwait(false));

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeId>> Register(IEnumerable<EventTypeToRegister> eventTypes)
    {
        var mutated = (await inner.Register(eventTypes).ConfigureAwait(false)).ToList();
        if (mutated.Count > 0)
        {
            Invalidate(mutated[0]);
        }

        return mutated;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetLatestForAllEventTypes() =>
        (await GetCurrent(false).ConfigureAwait(false)).Latest;

    /// <inheritdoc/>
    public ISubject<IEnumerable<EventTypeSchema>> ObserveLatestForAllEventTypes() => inner.ObserveLatestForAllEventTypes();

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeDefinition>> GetAllDefinitions() =>
        (await GetCurrent(false).ConfigureAwait(false)).Definitions;

    /// <inheritdoc/>
    public async Task<EventTypeDefinition> GetDefinition(EventTypeId eventTypeId)
    {
        var entry = await FindEntry(eventTypeId).ConfigureAwait(false);
        return entry is not null
            ? entry.Definition
            : await inner.GetDefinition(eventTypeId).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task RecordMigrationsVersion(EventTypeId eventTypeId, EventTypeMigrationsVersion version, IEnumerable<EventTypeMigrationDefinition> migrations) =>
        inner.RecordMigrationsVersion(eventTypeId, version, migrations);

    /// <inheritdoc/>
    public Task<IReadOnlyDictionary<EventTypeMigrationsVersion, IEnumerable<EventTypeMigrationDefinition>>> GetMigrationsVersions(EventTypeId eventTypeId) =>
        inner.GetMigrationsVersions(eventTypeId);

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeSchema>> GetAllGenerationsForEventType(EventType eventType) =>
        inner.GetAllGenerationsForEventType(eventType);

    /// <inheritdoc/>
    public async Task<bool> HasFor(EventTypeId type, EventTypeGeneration? generation = default)
    {
        if (generation is null)
        {
            return await inner.HasFor(type, generation).ConfigureAwait(false);
        }

        var entry = await FindEntry(type).ConfigureAwait(false);
        if (entry?.Generations.ContainsKey(generation) == true)
        {
            return true;
        }

        return await inner.HasFor(type, generation).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<EventTypeSchema> GetFor(EventTypeId type, EventTypeGeneration? generation = default)
    {
        if (generation is null)
        {
            return await inner.GetFor(type, generation).ConfigureAwait(false);
        }

        var entry = await FindEntry(type).ConfigureAwait(false);
        return entry is not null && entry.Generations.TryGetValue(generation, out var schema)
            ? schema
            : await inner.GetFor(type, generation).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<EventTypeId> eventTypeIds) => inner.GetFor(eventTypeIds);

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<EventType> eventTypes)
    {
        var requested = eventTypes.ToList();
        var snapshot = await GetCurrent(false).ConfigureAwait(false);
        if (requested.Exists(_ => !IsKnown(snapshot, _)))
        {
            snapshot = await GetCurrent(true).ConfigureAwait(false);
        }

        var resolved = new List<EventTypeSchema>(requested.Count);
        var missing = new List<EventType>();
        foreach (var eventType in requested)
        {
            if (snapshot.TryGetEntry(eventType.Id, out var entry) && entry.Generations.TryGetValue(eventType.Generation, out var schema))
            {
                resolved.Add(schema);
            }
            else
            {
                missing.Add(eventType);
            }
        }

        if (missing.Count > 0)
        {
            resolved.AddRange(await inner.GetFor(missing).ConfigureAwait(false));
        }

        return resolved;
    }

    static bool IsKnown(EventTypeRegistrySnapshot snapshot, EventType eventType) =>
        snapshot.TryGetEntry(eventType.Id, out var entry) && entry.Generations.ContainsKey(eventType.Generation);

    static EventTypeEntry CreateEntry(EventTypeDefinition definition, Dictionary<EventTypeId, EventTypeSchema> latestById)
    {
        var latestGeneration = definition.Generations.MaxBy(_ => _.Generation.Value)!;
        latestById.TryGetValue(definition.Id, out var stored);
        var latest = stored is not null && stored.Type.Generation == latestGeneration.Generation
            ? stored with { Schema = latestGeneration.Schema }
            : new EventTypeSchema(
                new EventType(definition.Id, latestGeneration.Generation, definition.Tombstone),
                definition.Owner,
                stored?.Source ?? EventTypeSource.Code,
                latestGeneration.Schema,
                stored?.Visibility ?? EventTypeVisibility.Unspecified,
                stored?.Origin ?? string.Empty);
        return new EventTypeEntry(definition, latest);
    }

    bool InvalidateIfMutated(EventTypeId eventTypeId, bool mutated)
    {
        if (mutated)
        {
            Invalidate(eventTypeId);
        }

        return mutated;
    }

    async Task<EventTypeEntry?> FindEntry(EventTypeId eventTypeId)
    {
        var snapshot = await GetCurrent(false).ConfigureAwait(false);
        if (snapshot.TryGetEntry(eventTypeId, out var entry))
        {
            return entry;
        }

        // Possibly registered by another silo whose announcement has not arrived yet.
        snapshot = await GetCurrent(true).ConfigureAwait(false);
        return snapshot.TryGetEntry(eventTypeId, out entry) ? entry : null;
    }

    bool IsCurrent(DateTimeOffset now, bool forMiss)
    {
        lock (_stamp)
        {
            return Volatile.Read(ref _snapshot) is not null &&
                _validatedAgainst == _invalidations &&
                now - _validatedAt < (forMiss ? MissReloadInterval : SafetyNetLifetime);
        }
    }

    async ValueTask<EventTypeRegistrySnapshot> GetCurrent(bool forMiss)
    {
        if (IsCurrent(timeProvider.GetUtcNow(), forMiss))
        {
            return Volatile.Read(ref _snapshot)!;
        }

        await _loading.WaitAsync().ConfigureAwait(false);
        try
        {
            // Another reader may have loaded while this one waited for the gate.
            var now = timeProvider.GetUtcNow();
            if (IsCurrent(now, forMiss))
            {
                return Volatile.Read(ref _snapshot)!;
            }

            long target;
            lock (_stamp)
            {
                target = _invalidations;
            }

            var loaded = await Load(Volatile.Read(ref _snapshot) ?? EventTypeRegistrySnapshot.Empty).ConfigureAwait(false);
            Volatile.Write(ref _snapshot, loaded);
            lock (_stamp)
            {
                _validatedAgainst = target;
                _validatedAt = now;
            }

            return loaded;
        }
        finally
        {
            _loading.Release();
        }
    }

    async Task<EventTypeRegistrySnapshot> Load(EventTypeRegistrySnapshot previous)
    {
        var definitions = (await inner.GetAllDefinitions().ConfigureAwait(false)).ToList();
        var latestById = new Dictionary<EventTypeId, EventTypeSchema>();
        foreach (var latest in await inner.GetLatestForAllEventTypes().ConfigureAwait(false))
        {
            latestById[latest.Type.Id] = latest;
        }

        var entries = new Dictionary<EventTypeId, EventTypeEntry>(definitions.Count);
        var changed = false;
        foreach (var definition in definitions.Where(_ => _.Generations.Any()))
        {
            var entry = CreateEntry(definition, latestById);
            if (previous.TryGetEntry(definition.Id, out var existing) && existing.IsSameAs(entry))
            {
                entry = existing;
            }
            else
            {
                changed = true;
            }

            entries[definition.Id] = entry;
        }

        changed |= entries.Count != previous.Entries.Count;
        return changed
            ? new EventTypeRegistrySnapshot(previous.Revision + 1, entries.ToFrozenDictionary())
            : previous;
    }
}
