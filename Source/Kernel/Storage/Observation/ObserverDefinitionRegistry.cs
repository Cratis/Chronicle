// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// The one place observer definitions are held: the silo-wide, immutable registry of the observer definitions of one
/// event store.
/// </summary>
/// <remarks>
/// <para>
/// This is what <see cref="IEventStoreStorage.Observers"/> hands out, wrapped around the plain storage of the provider.
/// An observer's definition is event-store-level data, so the observers of every namespace read the same
/// <see cref="ObserverDefinitionRegistrySnapshot"/> and hold the very same <see cref="ObserverDefinition"/> instance, with
/// its event types interned as an <see cref="EventTypeSet"/>. There is no storage read once it is loaded, and the storage
/// providers keep no cache of their own. What is persisted does not change: the registry saves and deletes through the
/// plain storage.
/// </para>
/// <para>
/// Freshness is settled per observer. A save or delete through the registry, and an <see cref="Invalidate"/> announced for
/// a change made by another silo, mark that observer as changed; the next reader reads just that one definition from
/// storage and publishes a new snapshot with it, so a store with thousands of observers is never reloaded because one of
/// them changed. Marks are versioned, so one that arrives while a refresh is in flight is not lost. A full load happens
/// once, when more than <see cref="MaxIncrementalRefreshes"/> observers are marked at the same time, and every
/// <see cref="SafetyNetLifetime"/>, which bounds how long a missed announcement can matter.
/// </para>
/// <para>
/// A lookup that finds nothing is answered by the wrapped storage, which keeps the behavior for unknown observers exactly
/// what the provider says it is, and an observer found that way is adopted into the registry.
/// </para>
/// </remarks>
/// <param name="inner">The plain <see cref="IObserverDefinitionsStorage"/> of the provider.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/> the lifetimes are measured with.</param>
public sealed class ObserverDefinitionRegistry(IObserverDefinitionsStorage inner, TimeProvider timeProvider) : IObserverDefinitionsStorage, IDisposable
{
    /// <summary>
    /// The most observers that are refreshed one by one before the whole event store is loaded instead.
    /// </summary>
    public const int MaxIncrementalRefreshes = 256;

    /// <summary>
    /// How long a snapshot is trusted without an announcement before it is loaded in full again.
    /// </summary>
    public static readonly TimeSpan SafetyNetLifetime = TimeSpan.FromMinutes(5);

    readonly SemaphoreSlim _refreshing = new(1, 1);
    readonly Lock _marks = new();
    readonly Dictionary<ObserverId, long> _changed = [];
    ObserverDefinitionRegistrySnapshot? _snapshot;
    DateTimeOffset _loadedAt;
    long _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverDefinitionRegistry"/> class.
    /// </summary>
    /// <param name="inner">The plain <see cref="IObserverDefinitionsStorage"/> of the provider.</param>
    public ObserverDefinitionRegistry(IObserverDefinitionsStorage inner)
        : this(inner, TimeProvider.System)
    {
    }

    /// <summary>
    /// Gets the current snapshot, loading or refreshing it when there is none or it is no longer current.
    /// </summary>
    /// <returns>The current <see cref="ObserverDefinitionRegistrySnapshot"/>.</returns>
    public ValueTask<ObserverDefinitionRegistrySnapshot> GetSnapshot() => GetCurrent();

    /// <summary>
    /// Reads the definition of an observer from storage, past the snapshot, to compare its revision with the one held.
    /// </summary>
    /// <param name="id">The <see cref="ObserverId"/> to read.</param>
    /// <returns>The stored <see cref="ObserverDefinition"/>, or the provider's answer for an unknown observer.</returns>
    public Task<ObserverDefinition> GetStored(ObserverId id) => inner.Get(id);

    /// <inheritdoc/>
    public void Dispose()
    {
        _refreshing.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public void Invalidate(ObserverId id)
    {
        lock (_marks)
        {
            _changed[id] = ++_clock;
        }
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<ObserverDefinition>> ObserveAll() => inner.ObserveAll();

    /// <inheritdoc/>
    public async Task<IEnumerable<ObserverDefinition>> GetAll() =>
        (await GetCurrent().ConfigureAwait(false)).Definitions.Values;

    /// <inheritdoc/>
    public async Task<bool> Has(ObserverId id) =>
        (await GetCurrent().ConfigureAwait(false)).Definitions.ContainsKey(id) || await inner.Has(id).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<ObserverDefinition> Get(ObserverId id)
    {
        var snapshot = await GetCurrent().ConfigureAwait(false);
        if (snapshot.TryGetDefinition(id, out var definition))
        {
            return definition;
        }

        // Possibly registered by another silo whose announcement has not arrived yet.
        var stored = await inner.Get(id).ConfigureAwait(false);
        if (!Exists(stored, id))
        {
            return stored;
        }

        Invalidate(id);
        snapshot = await GetCurrent().ConfigureAwait(false);
        return snapshot.TryGetDefinition(id, out definition) ? definition : stored;
    }

    /// <inheritdoc/>
    public async Task Delete(ObserverId id)
    {
        await inner.Delete(id).ConfigureAwait(false);
        Invalidate(id);
    }

    /// <inheritdoc/>
    public async Task Save(ObserverDefinition definition)
    {
        await inner.Save(await StampRevision(definition).ConfigureAwait(false)).ConfigureAwait(false);
        Invalidate(definition.Identifier);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ObserverDefinition>> GetForEventTypes(IEnumerable<EventType> eventTypes) => inner.GetForEventTypes(eventTypes);

    /// <inheritdoc/>
    public Task<IEnumerable<ObserverDefinition>> GetReplayableObserversForEventTypes(IEnumerable<EventType> eventTypes) =>
        inner.GetReplayableObserversForEventTypes(eventTypes);

    static bool Exists(ObserverDefinition definition, ObserverId id) => definition.Identifier == id;

    static ObserverDefinition Intern(ObserverDefinition definition) =>
        definition with { EventTypes = EventTypeSet.From(definition.EventTypes) };

    static ObserverDefinition Share(ObserverDefinitionRegistrySnapshot previous, ObserverDefinition definition) =>
        previous.TryGetDefinition(definition.Identifier, out var existing) && existing.Equals(definition) ? existing : definition;

    /// <summary>
    /// Gives a definition about to be saved its revision: the stored one when the content is unchanged, the next one otherwise.
    /// </summary>
    /// <param name="definition">The <see cref="ObserverDefinition"/> about to be saved.</param>
    /// <returns>The definition with its <see cref="ObserverDefinition.Revision"/> set.</returns>
    /// <remarks>
    /// The revision is read from storage, never from the snapshot, so a change made by another silo whose announcement has
    /// not arrived yet is never given a revision that is already taken.
    /// </remarks>
    async Task<ObserverDefinition> StampRevision(ObserverDefinition definition)
    {
        var stored = await inner.Get(definition.Identifier).ConfigureAwait(false);
        if (!Exists(stored, definition.Identifier))
        {
            return definition with { Revision = ObserverDefinitionRevision.None.Next() };
        }

        return definition with { Revision = stored.HasSameContentAs(definition) ? stored.Revision : stored.Revision.Next() };
    }

    bool IsCurrent(DateTimeOffset now)
    {
        lock (_marks)
        {
            return Volatile.Read(ref _snapshot) is not null && _changed.Count == 0 && now - _loadedAt < SafetyNetLifetime;
        }
    }

    async ValueTask<ObserverDefinitionRegistrySnapshot> GetCurrent()
    {
        if (IsCurrent(timeProvider.GetUtcNow()))
        {
            return Volatile.Read(ref _snapshot)!;
        }

        await _refreshing.WaitAsync().ConfigureAwait(false);
        try
        {
            // Another reader may have refreshed while this one waited for the gate.
            var now = timeProvider.GetUtcNow();
            if (IsCurrent(now))
            {
                return Volatile.Read(ref _snapshot)!;
            }

            KeyValuePair<ObserverId, long>[] marked;
            long target;
            lock (_marks)
            {
                marked = [.. _changed];
                target = _clock;
            }

            var previous = Volatile.Read(ref _snapshot);
            var full = previous is null || now - _loadedAt >= SafetyNetLifetime || marked.Length > MaxIncrementalRefreshes;
            var refreshed = full
                ? await Load(previous ?? ObserverDefinitionRegistrySnapshot.Empty).ConfigureAwait(false)
                : await Refresh(previous!, marked.Select(_ => _.Key)).ConfigureAwait(false);

            Volatile.Write(ref _snapshot, refreshed);
            lock (_marks)
            {
                foreach (var (id, version) in marked)
                {
                    if (_changed.TryGetValue(id, out var current) && current == version)
                    {
                        _changed.Remove(id);
                    }
                }

                // A full load covers everything marked before it started, whatever the number was.
                if (full)
                {
                    foreach (var id in _changed.Where(_ => _.Value <= target).Select(_ => _.Key).ToArray())
                    {
                        _changed.Remove(id);
                    }

                    _loadedAt = now;
                }
            }

            return refreshed;
        }
        finally
        {
            _refreshing.Release();
        }
    }

    async Task<ObserverDefinitionRegistrySnapshot> Load(ObserverDefinitionRegistrySnapshot previous)
    {
        var definitions = new Dictionary<ObserverId, ObserverDefinition>();
        foreach (var definition in await inner.GetAll().ConfigureAwait(false))
        {
            definitions[definition.Identifier] = Share(previous, Intern(definition));
        }

        var changed = definitions.Count != previous.Definitions.Count ||
            definitions.Any(_ => !previous.TryGetDefinition(_.Key, out var existing) || !ReferenceEquals(existing, _.Value));

        return changed
            ? new ObserverDefinitionRegistrySnapshot(previous.Revision + 1, definitions.ToFrozenDictionary())
            : previous;
    }

    async Task<ObserverDefinitionRegistrySnapshot> Refresh(ObserverDefinitionRegistrySnapshot previous, IEnumerable<ObserverId> ids)
    {
        var definitions = new Dictionary<ObserverId, ObserverDefinition>(previous.Definitions);
        var changed = false;
        foreach (var id in ids)
        {
            var stored = await inner.Get(id).ConfigureAwait(false);
            if (Exists(stored, id))
            {
                var definition = Share(previous, Intern(stored));
                if (!previous.TryGetDefinition(id, out var existing) || !ReferenceEquals(existing, definition))
                {
                    definitions[id] = definition;
                    changed = true;
                }
            }
            else if (definitions.Remove(id))
            {
                changed = true;
            }
        }

        return changed
            ? new ObserverDefinitionRegistrySnapshot(previous.Revision + 1, definitions.ToFrozenDictionary())
            : previous;
    }
}
