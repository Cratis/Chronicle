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
/// <param name="database">The <see cref="IEventStoreNamespaceDatabase"/> to use.</param>
/// <param name="eventSequenceId">The <see cref="EventSequenceId"/> to use.</param>
public class UniqueEventTypesConstraintsStorage(IEventStoreNamespaceDatabase database, EventSequenceId eventSequenceId) : IUniqueEventTypesConstraintsStorage
{
    readonly IMongoCollection<Event> _collection = database.GetEventSequenceCollectionFor(eventSequenceId);

    /// <inheritdoc/>
    public async Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(UniqueEventTypeConstraintDefinition definition, EventSourceId eventSourceId, string scopeKey = "")
    {
        var forEventSource = Builders<Event>.Filter.Eq(_ => _.EventSourceId, eventSourceId);
        var filter = forEventSource & Builders<Event>.Filter.In(_ => _.Type, definition.EventTypeIds);

        var latestRemoval = await GetLatestRemoval(definition, forEventSource);
        if (latestRemoval is not null)
        {
            filter &= Builders<Event>.Filter.Gt(_ => _.SequenceNumber, latestRemoval);
        }

        // Ordered so the sequence number reported back is the covered event that actually holds the cycle, rather
        // than whichever one the collection happened to yield first.
        var existing = await _collection.Find(filter).SortBy(_ => _.SequenceNumber).FirstOrDefaultAsync();
        if (existing is not null)
        {
            return (false, existing.SequenceNumber);
        }

        return (true, EventSequenceNumber.Unavailable);
    }

    /// <summary>
    /// Find the most recent event on the event source that releases the constraint.
    /// </summary>
    /// <param name="definition">The <see cref="UniqueEventTypeConstraintDefinition"/> to read the removal events from.</param>
    /// <param name="forEventSource">Filter narrowing to the event source being answered for.</param>
    /// <returns>The <see cref="EventSequenceNumber"/> the current cycle starts after, or <see langword="null"/> when nothing released it.</returns>
    /// <remarks>
    /// Any of the declared removal events ends a cycle, so the latest across all of them is the one that counts —
    /// looking at only one of them would keep answering against a cycle that another terminal fact already closed.
    /// </remarks>
    async Task<EventSequenceNumber?> GetLatestRemoval(UniqueEventTypeConstraintDefinition definition, FilterDefinition<Event> forEventSource)
    {
        var removalEventTypeIds = definition.RemovedWith.ToArray();
        if (removalEventTypeIds.Length == 0)
        {
            return null;
        }

        var filter = forEventSource & Builders<Event>.Filter.In(_ => _.Type, removalEventTypeIds);
        var latest = await _collection.Find(filter).SortByDescending(_ => _.SequenceNumber).FirstOrDefaultAsync();
        return latest?.SequenceNumber;
    }
}
