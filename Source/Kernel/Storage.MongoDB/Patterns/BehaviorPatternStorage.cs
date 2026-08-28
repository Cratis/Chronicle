// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Storage.Patterns;
using MongoDB.Driver;
using KernelBehaviorPattern = Cratis.Chronicle.Concepts.Patterns.BehaviorPattern;

namespace Cratis.Chronicle.Storage.MongoDB.Patterns;

/// <summary>
/// Represents a MongoDB implementation of <see cref="IBehaviorPatternStorage"/>.
/// </summary>
/// <param name="database">The <see cref="IEventStoreNamespaceDatabase"/> the patterns are held in.</param>
public class BehaviorPatternStorage(IEventStoreNamespaceDatabase database) : IBehaviorPatternStorage
{
    IMongoCollection<BehaviorPattern> Collection => database.GetCollection<BehaviorPattern>(WellKnownCollectionNames.BehaviorPatterns);

    /// <inheritdoc/>
    public async Task Save(IEnumerable<KernelBehaviorPattern> patterns)
    {
        var writes = patterns
            .Select(pattern => pattern.ToMongoDB())
            .Select(document => new ReplaceOneModel<BehaviorPattern>(
                Builders<BehaviorPattern>.Filter.Eq(_ => _.Id, document.Id),
                document)
            {
                IsUpsert = true
            })
            .ToArray();

        if (writes.Length == 0)
        {
            return;
        }

        await Collection.BulkWriteAsync(writes);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<KernelBehaviorPattern>> GetForScope(PatternGroupingKey groupingKey)
    {
        using var cursor = await Collection.FindAsync(Builders<BehaviorPattern>.Filter.Eq(_ => _.GroupingKey, groupingKey));
        var patterns = await cursor.ToListAsync();
        return [.. patterns.Select(pattern => pattern.ToKernel())];
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<KernelBehaviorPattern>> GetMatching(PatternGroupingKey groupingKey, IEnumerable<FacetSetKey> candidates)
    {
        var keys = candidates.Distinct().ToArray();
        if (keys.Length == 0)
        {
            return [];
        }

        var filter = Builders<BehaviorPattern>.Filter.And(
            Builders<BehaviorPattern>.Filter.Eq(_ => _.GroupingKey, groupingKey),
            Builders<BehaviorPattern>.Filter.In(_ => _.FacetSetKey, keys));

        using var cursor = await Collection.FindAsync(filter);
        var patterns = await cursor.ToListAsync();
        return [.. patterns.Select(pattern => pattern.ToKernel())];
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PatternGroupingKey>> GetScopes()
    {
        using var cursor = await Collection.DistinctAsync(_ => _.GroupingKey, FilterDefinition<BehaviorPattern>.Empty);
        return await cursor.ToListAsync();
    }

    /// <inheritdoc/>
    public Task RemoveAllExcept(PatternGroupingKey groupingKey, IEnumerable<FacetSetKey> surviving)
    {
        var filter = Builders<BehaviorPattern>.Filter.And(
            Builders<BehaviorPattern>.Filter.Eq(_ => _.GroupingKey, groupingKey),
            Builders<BehaviorPattern>.Filter.Nin(_ => _.FacetSetKey, surviving.Distinct()));

        return Collection.DeleteManyAsync(filter);
    }
}
