// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Storage.Patterns;
using Microsoft.EntityFrameworkCore;
using KernelBehaviorPattern = Cratis.Chronicle.Concepts.Patterns.BehaviorPattern;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Patterns;

/// <summary>
/// Represents an implementation of <see cref="IBehaviorPatternStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="namespace">The name of the namespace.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class BehaviorPatternStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, IDatabase database) : IBehaviorPatternStorage
{
    readonly BehaviorPatternConverter _converter = new();

    /// <inheritdoc/>
    public async Task Save(IEnumerable<KernelBehaviorPattern> patterns)
    {
        var entities = patterns.Select(_converter.ToEntity).ToArray();
        if (entities.Length == 0)
        {
            return;
        }

        await using var scope = await database.Namespace(eventStore, @namespace);
        foreach (var entity in entities)
        {
            await scope.DbContext.BehaviorPatterns.Upsert(entity);
        }

        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<KernelBehaviorPattern>> GetForScope(PatternGroupingKey groupingKey)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var entities = await scope.DbContext.BehaviorPatterns
            .Where(pattern => pattern.GroupingKey == groupingKey.Value)
            .ToListAsync();

        return [.. entities.Select(_converter.ToKernel)];
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<KernelBehaviorPattern>> GetMatching(PatternGroupingKey groupingKey, IEnumerable<FacetSetKey> candidates)
    {
        var hashes = candidates.Distinct().Select(FacetSetHash.Of).ToArray();
        if (hashes.Length == 0)
        {
            return [];
        }

        await using var scope = await database.Namespace(eventStore, @namespace);
        var entities = await scope.DbContext.BehaviorPatterns
            .Where(pattern => pattern.GroupingKey == groupingKey.Value && hashes.Contains(pattern.FacetSetHash))
            .ToListAsync();

        return [.. entities.Select(_converter.ToKernel)];
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PatternGroupingKey>> GetScopes()
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var keys = await scope.DbContext.BehaviorPatterns
            .Select(pattern => pattern.GroupingKey)
            .Distinct()
            .ToListAsync();

        return [.. keys.Select(key => new PatternGroupingKey(key))];
    }

    /// <inheritdoc/>
    public async Task RemoveAllExcept(PatternGroupingKey groupingKey, IEnumerable<FacetSetKey> surviving)
    {
        var hashes = surviving.Distinct().Select(FacetSetHash.Of).ToArray();

        await using var scope = await database.Namespace(eventStore, @namespace);
        var doomed = await scope.DbContext.BehaviorPatterns
            .Where(pattern => pattern.GroupingKey == groupingKey.Value && !hashes.Contains(pattern.FacetSetHash))
            .ToListAsync();

        if (doomed.Count == 0)
        {
            return;
        }

        scope.DbContext.BehaviorPatterns.RemoveRange(doomed);
        await scope.DbContext.SaveChangesAsync();
    }
}
