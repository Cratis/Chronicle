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
    public async Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value)
    {
        var collection = GetCollectionFor(definition.Name);
        var options = new FindOptions<UniqueConstraintIndex, UniqueConstraintIndex>();

        if (definition.IgnoreCasing)
        {
            options.Collation = new Collation("en", caseLevel: false, strength: CollationStrength.Secondary);
        }

        using var result = await collection.FindAsync(_ => _.Value == value, options);
        var existing = result.FirstOrDefault();
        if (existing is not null)
        {
            if (existing.EventSourceId == eventSourceId) return (true, existing.SequenceNumber);

            return (false, existing.SequenceNumber);
        }

        return (true, EventSequenceNumber.Unavailable);
    }

    /// <inheritdoc/>
    public async Task Save(EventSourceId eventSourceId, ConstraintName name, EventSequenceNumber sequenceNumber, UniqueConstraintValue value)
    {
        var collection = GetCollectionFor(name);
        await collection.ReplaceOneAsync(
            u => u.EventSourceId == eventSourceId,
            new UniqueConstraintIndex(eventSourceId, value, sequenceNumber),
            new ReplaceOptions { IsUpsert = true });
    }

    /// <inheritdoc/>
    public async Task Remove(EventSourceId eventSourceId, ConstraintName name)
    {
        var collection = GetCollectionFor(name);
        await collection.DeleteOneAsync(u => u.EventSourceId == eventSourceId);
    }

    IMongoCollection<UniqueConstraintIndex> GetCollectionFor(ConstraintName constraintName) =>
        eventStoreNamespaceDatabase.GetCollection<UniqueConstraintIndex>($"{eventSequenceId}+{constraintName}+constraint");
}
