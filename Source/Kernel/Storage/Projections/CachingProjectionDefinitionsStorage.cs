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
/// Projection definitions are read repeatedly and change rarely: <c>Has</c> and <c>Get</c> run on every projection grain
/// activation and <c>GetAll</c> on every projections-manager activation. This cache serves those reads from memory after
/// the first load, avoiding a backing-store round-trip per call while keeping writes correct through write-through on
/// <see cref="Save"/> and eviction on <see cref="Delete"/>.
/// </para>
/// <para>
/// <b>Staleness / eventual consistency:</b> within a single silo every read and write routes through this same cached
/// instance, so local write-through keeps the cache authoritative for that silo. Across a cluster, a <see cref="Save"/> or
/// <see cref="Delete"/> performed on another silo leaves this silo's cache stale until it is discarded. This is acceptable
/// because the <c>Projection</c> grain is single-activation per (event store, projection id) cluster-wide and is the
/// authoritative writer holding the definition in grain state — this cache is only a read-accelerator for activation and
/// listing and is intentionally eventually consistent. Cross-silo invalidation is deliberately out of scope for this cache.
/// </para>
/// </remarks>
/// <param name="inner">The inner <see cref="IProjectionDefinitionsStorage"/> the cache delegates to.</param>
public sealed class CachingProjectionDefinitionsStorage(IProjectionDefinitionsStorage inner) : IProjectionDefinitionsStorage
{
    readonly ConcurrentDictionary<ProjectionId, ProjectionDefinition> _cache = new();
    volatile bool _allLoaded;

    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectionDefinition>> GetAll()
    {
        if (_allLoaded)
        {
            return _cache.Values.ToArray();
        }

        var definitions = await inner.GetAll();
        foreach (var definition in definitions)
        {
            _cache[definition.Identifier] = definition;
        }

        _allLoaded = true;
        return _cache.Values.ToArray();
    }

    /// <inheritdoc/>
    public async Task<bool> Has(ProjectionId id)
    {
        if (_cache.ContainsKey(id))
        {
            return true;
        }

        if (_allLoaded)
        {
            return false;
        }

        return await inner.Has(id);
    }

    /// <inheritdoc/>
    public async Task<ProjectionDefinition> Get(ProjectionId id)
    {
        if (_cache.TryGetValue(id, out var cached))
        {
            return cached;
        }

        var definition = await inner.Get(id);
        _cache[id] = definition;
        return definition;
    }

    /// <inheritdoc/>
    public async Task Delete(ProjectionId id)
    {
        await inner.Delete(id);
        _cache.TryRemove(id, out _);
    }

    /// <inheritdoc/>
    public async Task Save(ProjectionDefinition definition)
    {
        await inner.Save(definition);
        _cache[definition.Identifier] = definition;
    }
}
