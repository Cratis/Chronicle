// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences;

public partial class EventSequenceStorage
{
    async Task<AppendManyStorageFailure?> ResolveNamespaceCollision(IClientSessionHandle session, EventToAppendToStorage[] batch, bool persistIdentities, CancellationToken cancellationToken)
    {
        if (persistIdentities)
        {
            var ids = batch.Where(item => item.Identity is not null).Select(item => item.Identity!.Id).ToArray();
            var documents = await _collection.Find(session, Builders<Event>.Filter.In(item => item.PublicationId, ids)).ToListAsync(cancellationToken);
            if (documents.Count > 0)
            {
                return new IdentitiesAlreadyDurable(documents.ToDictionary(item => item.PublicationId!, item => new StoredEventPublication(item.SequenceNumber, item.PublicationFingerprint!), StringComparer.Ordinal));
            }
        }

        var numbers = batch.Select(item => item.SequenceNumber).ToArray();
        if (numbers.Distinct().Count() == numbers.Length && !await _collection.Find(session, Builders<Event>.Filter.In(item => item.SequenceNumber, numbers)).AnyAsync(cancellationToken)) return null;
        var highest = await _collection.Find(session, FilterDefinition<Event>.Empty).SortByDescendingSequenceNumber().Limit(1).SingleOrDefaultAsync(cancellationToken);
        return new DuplicateEventSequenceNumber(highest?.SequenceNumber.Next() ?? EventSequenceNumber.First);
    }
}
