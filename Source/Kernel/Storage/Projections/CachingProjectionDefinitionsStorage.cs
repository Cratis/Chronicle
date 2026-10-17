// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionDefinitionsStorage"/> that works as a local read-through cache
/// in front of another <see cref="IProjectionDefinitionsStorage"/>.
/// </summary>
/// <remarks>
/// <para>
/// Projection definitions are read repeatedly and change rarely: <c language="csharp">Has</c> and <c language="csharp">Get</c> run on every projection grain
/// activation. This cache serves those reads from memory once a definition is known, avoiding a backing-store round-trip
/// per call while keeping writes correct through write-through on <see cref="Save"/> and eviction on <see cref="Delete"/>.
/// </para>
/// <para>
/// <b>Consistency across silos:</b> every silo has its own instance of this cache over the same shared storage, and a
/// <see cref="Save"/> or <see cref="Delete"/> on one silo does not reach the caches of the others. A definition missing
/// from this cache is therefore never treated as absent: <see cref="Has"/> and <see cref="Get"/> fall through to the
/// shared storage on a miss, and <see cref="GetAll"/> always lists the shared storage, refreshing the cache from it and dropping cached definitions it no
/// longer contains. A listing or read that overlaps a local <see cref="Save"/> or <see cref="Delete"/> leaves the cache
/// untouched, so an older snapshot never overwrites a newer local write. A cached definition can still be stale when another silo changed or deleted it; the
/// <c language="csharp">Projection</c> grain, single-activation per (event store, projection id) cluster-wide, is the authoritative
/// writer of its definition. Legacy entries are always re-read because their unknown defaults must not hide a
/// lossless upgrade already committed by another silo.
/// </para>
/// </remarks>
/// <param name="inner">The inner <see cref="IProjectionDefinitionsStorage"/> the cache delegates to.</param>
public sealed class CachingProjectionDefinitionsStorage(IProjectionDefinitionsStorage inner) : IProjectionDefinitionsStorage
{
    readonly ConcurrentDictionary<ProjectionId, ProjectionDefinition> _cache = new();
    readonly Lock _lock = new();
    long _mutations;

    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectionDefinition>> GetAll()
    {
        var mutationsBefore = Interlocked.Read(ref _mutations);
        var definitions = (await inner.GetAll()).ToArray();

        lock (_lock)
        {
            // A Save or Delete that completed while the shared storage was being listed is newer than this snapshot.
            // Leaving the cache alone is safe: entries missing from it are read from the shared storage.
            if (_mutations != mutationsBefore)
            {
                return definitions;
            }

            var identifiers = definitions.Select(definition => definition.Identifier).ToHashSet();
            foreach (var removed in _cache.Keys.Where(identifier => !identifiers.Contains(identifier)))
            {
                _cache.TryRemove(removed, out _);
            }

            foreach (var definition in definitions)
            {
                _cache[definition.Identifier] = definition;
            }
        }

        return definitions;
    }

    /// <inheritdoc/>
    public async Task<bool> Has(ProjectionId id) => _cache.ContainsKey(id) || await inner.Has(id);

    /// <inheritdoc/>
    public async Task<ProjectionDefinition> Get(ProjectionId id)
    {
        // Legacy defaults are not authoritative: another silo may already have upgraded this definition.
        if (_cache.TryGetValue(id, out var cached) && !cached.HasLegacyStorageFields)
        {
            return cached;
        }

        var mutationsBefore = Interlocked.Read(ref _mutations);
        var definition = await inner.Get(id);
        lock (_lock)
        {
            if (_mutations == mutationsBefore)
            {
                _cache[id] = definition;
            }
        }

        return definition;
    }

    /// <inheritdoc/>
    public async Task Delete(ProjectionId id)
    {
        await inner.Delete(id);
        lock (_lock)
        {
            Interlocked.Increment(ref _mutations);
            _cache.TryRemove(id, out _);
        }
    }

    /// <inheritdoc/>
    public async Task Save(ProjectionDefinition definition)
    {
        await inner.Save(definition);
        lock (_lock)
        {
            Interlocked.Increment(ref _mutations);
            _cache[definition.Identifier] = definition;
        }
    }
}
