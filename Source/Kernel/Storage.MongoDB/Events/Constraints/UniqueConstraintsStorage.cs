// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Events.Constraints;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IUniqueConstraintsStorage"/>.
/// </summary>
/// <param name="eventStoreNamespaceDatabase"><see cref="IEventStoreNamespaceDatabase"/> for the storage.</param>
/// <param name="eventSequenceId"><see cref="EventSequenceId"/> for the storage.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class UniqueConstraintsStorage(
    IEventStoreNamespaceDatabase eventStoreNamespaceDatabase,
    EventSequenceId eventSequenceId,
    ILogger<UniqueConstraintsStorage> logger) : IUniqueConstraintsStorage
{
    const string ValueIndexName = "value";
    readonly ConcurrentDictionary<string, byte> _ensuredIndexes = new();

    /// <inheritdoc/>
    public async Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, string scopeKey = "")
    {
        var collection = GetCollectionFor(definition.Name, scopeKey);
        await EnsureIndex(collection).ConfigureAwait(false);

        // Note: Case-insensitive comparison is now handled by hashing the value with case normalization
        // before it reaches the storage layer, so we can use a simple equality check here.
        using var result = await collection.FindAsync(_ => _.Value == value);
        var existing = await result.FirstOrDefaultAsync();
        if (existing is not null)
        {
            if (existing.EventSourceId == eventSourceId) return (true, existing.SequenceNumber);

            return (false, existing.SequenceNumber);
        }

        return (true, EventSequenceNumber.Unavailable);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The unique index on the value is what settles a claim that two event sources make concurrently, and it
    /// reports the loser as a duplicate-key write error. That is a constraint violation, not a storage malfunction,
    /// so it is translated into <see cref="DuplicateUniqueConstraintValue"/> rather than surfacing a driver
    /// exception callers would have to recognize. The translation is diagnostic only - see the remarks on
    /// <see cref="DuplicateUniqueConstraintValue"/> for what a caller can and cannot do with it.
    /// <para>
    /// It also only fires while the index is genuinely unique. <see cref="EnsureIndex"/> falls back to a non-unique
    /// index when the collection already holds duplicate values from before uniqueness was enforced, and while that
    /// fallback is in place the store settles nothing and no duplicate-key error is ever raised.
    /// </para>
    /// </remarks>
    public async Task Save(EventSourceId eventSourceId, ConstraintName name, EventSequenceNumber sequenceNumber, UniqueConstraintValue value, string scopeKey = "")
    {
        var collection = GetCollectionFor(name, scopeKey);
        await EnsureIndex(collection).ConfigureAwait(false);
        try
        {
            await collection.ReplaceOneAsync(
                u => u.EventSourceId == eventSourceId,
                new UniqueConstraintIndex(eventSourceId, value, sequenceNumber),
                new ReplaceOptions { IsUpsert = true });
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new DuplicateUniqueConstraintValue(name, eventSourceId);
        }
    }

    /// <inheritdoc/>
    public async Task Remove(EventSourceId eventSourceId, ConstraintName name, string scopeKey = "")
    {
        var collection = GetCollectionFor(name, scopeKey);
        await collection.DeleteOneAsync(u => u.EventSourceId == eventSourceId);
    }

    static Task<string> CreateValueIndex(IMongoCollection<UniqueConstraintIndex> collection, bool unique) =>
        collection.Indexes.CreateOneAsync(
            new CreateIndexModel<UniqueConstraintIndex>(
                Builders<UniqueConstraintIndex>.IndexKeys.Ascending(_ => _.Value),
                new CreateIndexOptions { Name = ValueIndexName, Unique = unique, Background = true }));

    IMongoCollection<UniqueConstraintIndex> GetCollectionFor(ConstraintName constraintName, string scopeKey = "")
    {
        var collectionName = string.IsNullOrEmpty(scopeKey)
            ? $"{eventSequenceId}+{constraintName}+constraint"
            : $"{eventSequenceId}+{constraintName}+{scopeKey}+constraint";
        return eventStoreNamespaceDatabase.GetCollection<UniqueConstraintIndex>(collectionName);
    }

    async Task EnsureIndex(IMongoCollection<UniqueConstraintIndex> collection)
    {
        if (_ensuredIndexes.ContainsKey(collection.CollectionNamespace.FullName))
        {
            return;
        }

        var existing = await collection.GetIndexNamesAsync().ConfigureAwait(false);
        if (!existing.Contains(ValueIndexName))
        {
            try
            {
                await CreateValueIndex(collection, unique: true).ConfigureAwait(false);
            }
            catch (MongoCommandException ex) when (ex.Code == 11000 || ex.Message.Contains("E11000", StringComparison.Ordinal))
            {
                // The collection already contains duplicate values from before the unique index existed, so the
                // unique index cannot be built. Fall back to a non-unique index so lookups are still fast; the
                // stored duplicates need to be reconciled before uniqueness can be enforced again.
                logger.FallingBackToNonUniqueIndex(collection.CollectionNamespace.FullName);
                await CreateValueIndex(collection, unique: false).ConfigureAwait(false);
            }
        }

        _ensuredIndexes.TryAdd(collection.CollectionNamespace.FullName, 0);
    }
}
