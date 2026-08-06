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

    async Task<EventSequenceNumber?> GetLatestRemoval(UniqueEventTypeConstraintDefinition definition, FilterDefinition<Event> forEventSource)
    {
        if (definition.RemovedWith is null)
        {
            return null;
        }

        var filter = forEventSource & Builders<Event>.Filter.Eq(_ => _.Type, definition.RemovedWith);
        var latest = await _collection.Find(filter).SortByDescending(_ => _.SequenceNumber).FirstOrDefaultAsync();
        return latest?.SequenceNumber;
    }
}
