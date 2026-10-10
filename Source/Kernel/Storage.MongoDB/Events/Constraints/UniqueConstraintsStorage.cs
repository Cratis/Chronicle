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
        if (definition.Mode == UniqueConstraintMode.PerValue)
        {
            var values = GetValuesCollectionFor(definition.Name, scopeKey);
            await EnsureOwnerIndex(values);
            using var found = await values.FindAsync(_ => _.Value == value);
            var entry = await found.FirstOrDefaultAsync();
            return entry is null ? (true, EventSequenceNumber.Unavailable) : (entry.EventSourceId == eventSourceId, entry.SequenceNumber);
        }

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

    /// <summary>
    /// Saves a claim using the definition's retention mode.
    /// </summary>
    /// <param name="eventSourceId">The owner.</param>
    /// <param name="definition">The constraint.</param>
    /// <param name="sequenceNumber">The claim's sequence number.</param>
    /// <param name="value">The hashed value.</param>
    /// <param name="scopeKey">The resolved scope.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="DuplicateUniqueConstraintValue">Another event source owns the value.</exception>
    public async Task Save(EventSourceId eventSourceId, UniqueConstraintDefinition definition, EventSequenceNumber sequenceNumber, UniqueConstraintValue value, string scopeKey = "")
    {
        if (definition.Mode != UniqueConstraintMode.PerValue)
        {
            await SavePerEventSource(eventSourceId, definition.Name, sequenceNumber, value, scopeKey);
            return;
        }

        var collection = GetValuesCollectionFor(definition.Name, scopeKey);
        await EnsureOwnerIndex(collection);
        try
        {
            await collection.UpdateOneAsync(
                _ => _.Value == value && _.EventSourceId == eventSourceId,
                Builders<UniqueConstraintValueIndex>.Update
                    .SetOnInsert(_ => _.Value, value)
                    .SetOnInsert(_ => _.EventSourceId, eventSourceId)
                    .SetOnInsert(_ => _.SequenceNumber, sequenceNumber),
                new UpdateOptions { IsUpsert = true });
        }
        catch (MongoWriteException error) when (error.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            using var found = await collection.FindAsync(_ => _.Value == value);
            var winner = await found.FirstOrDefaultAsync();
            if (winner?.EventSourceId != eventSourceId)
            {
                throw new DuplicateUniqueConstraintValue(definition.Name, eventSourceId);
            }
        }
    }

    /// <summary>
    /// Releases every claim held by the source in the selected scope.
    /// </summary>
    /// <param name="eventSourceId">The owner.</param>
    /// <param name="definition">The constraint.</param>
    /// <param name="scopeKey">The resolved scope.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Remove(EventSourceId eventSourceId, UniqueConstraintDefinition definition, string scopeKey = "")
    {
        if (definition.Mode == UniqueConstraintMode.PerValue)
        {
            await GetValuesCollectionFor(definition.Name, scopeKey).DeleteManyAsync(_ => _.EventSourceId == eventSourceId);
        }
        else
        {
            await GetCollectionFor(definition.Name, scopeKey).DeleteOneAsync(_ => _.EventSourceId == eventSourceId);
        }
    }

    /// <summary>
    /// Releases one value only if the source owns it.
    /// </summary>
    /// <param name="eventSourceId">The owner.</param>
    /// <param name="definition">The constraint.</param>
    /// <param name="value">The value to release.</param>
    /// <param name="scopeKey">The resolved scope.</param>
    /// <returns>Awaitable task.</returns>
    public async Task RemoveValue(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, string scopeKey = "") =>
        await GetValuesCollectionFor(definition.Name, scopeKey).DeleteOneAsync(_ => _.Value == value && _.EventSourceId == eventSourceId);

    static Task<string> CreateValueIndex(IMongoCollection<UniqueConstraintIndex> collection, bool unique) =>
        collection.Indexes.CreateOneAsync(
            new CreateIndexModel<UniqueConstraintIndex>(
                Builders<UniqueConstraintIndex>.IndexKeys.Ascending(_ => _.Value),
                new CreateIndexOptions { Name = ValueIndexName, Unique = unique, Background = true }));

    async Task SavePerEventSource(EventSourceId eventSourceId, ConstraintName name, EventSequenceNumber sequenceNumber, UniqueConstraintValue value, string scopeKey)
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

    IMongoCollection<UniqueConstraintValueIndex> GetValuesCollectionFor(ConstraintName name, string scopeKey) =>
        eventStoreNamespaceDatabase.GetCollection<UniqueConstraintValueIndex>(string.IsNullOrEmpty(scopeKey)
            ? $"{eventSequenceId}+{name}+values+constraint"
            : $"{eventSequenceId}+{name}+{scopeKey}+values+constraint");

    async Task EnsureOwnerIndex(IMongoCollection<UniqueConstraintValueIndex> collection)
    {
        if (_ensuredIndexes.ContainsKey(collection.CollectionNamespace.FullName))
        {
            return;
        }

        await collection.Indexes.CreateOneAsync(new CreateIndexModel<UniqueConstraintValueIndex>(
            Builders<UniqueConstraintValueIndex>.IndexKeys.Ascending(_ => _.EventSourceId),
            new CreateIndexOptions { Name = "owner" }));
        _ensuredIndexes.TryAdd(collection.CollectionNamespace.FullName, 0);
    }

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
