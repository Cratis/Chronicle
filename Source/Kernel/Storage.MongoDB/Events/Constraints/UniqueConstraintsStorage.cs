// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Events.Constraints;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IUniqueConstraintsStorage"/>.
/// </summary>
/// <param name="eventStoreNamespaceDatabase"><see cref="IEventStoreNamespaceDatabase"/> for the storage.</param>
/// <param name="eventSequenceId"><see cref="EventSequenceId"/> for the storage.</param>
public class UniqueConstraintsStorage(IEventStoreNamespaceDatabase eventStoreNamespaceDatabase, EventSequenceId eventSequenceId) : IUniqueConstraintsStorage
{
    /// <inheritdoc/>
    public async Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, string scopeKey = "")
    {
        var collection = GetCollectionFor(definition.Name, scopeKey);

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
    public async Task Save(EventSourceId eventSourceId, ConstraintName name, EventSequenceNumber sequenceNumber, UniqueConstraintValue value, string scopeKey = "")
    {
        var collection = GetCollectionFor(name, scopeKey);
        await collection.ReplaceOneAsync(
            u => u.EventSourceId == eventSourceId,
            new UniqueConstraintIndex(eventSourceId, value, sequenceNumber),
            new ReplaceOptions { IsUpsert = true });
    }

    /// <inheritdoc/>
    public async Task Remove(EventSourceId eventSourceId, ConstraintName name, string scopeKey = "")
    {
        var collection = GetCollectionFor(name, scopeKey);
        await collection.DeleteOneAsync(u => u.EventSourceId == eventSourceId);
    }

    IMongoCollection<UniqueConstraintIndex> GetCollectionFor(ConstraintName constraintName, string scopeKey = "")
    {
        var collectionName = string.IsNullOrEmpty(scopeKey)
            ? $"{eventSequenceId}+{constraintName}+constraint"
            : $"{eventSequenceId}+{constraintName}+{scopeKey}+constraint";
        return eventStoreNamespaceDatabase.GetCollection<UniqueConstraintIndex>(collectionName);
    }
}
