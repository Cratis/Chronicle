// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences;

public partial class EventSequenceStorage
{
    /// <inheritdoc/>
    public Task<Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>> AppendManyWithIdentities(IEnumerable<EventToAppendToStorage> events) => AppendBatch(events, true);

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, StoredEventPublication>> TryGetPublications(IEnumerable<string> identities)
    {
        var ids = identities.Distinct(StringComparer.Ordinal).ToArray();
        var documents = await _collection.Find(Builders<Event>.Filter.In(item => item.PublicationId, ids)).ToListAsync().ConfigureAwait(false);
        return documents.ToDictionary(item => item.PublicationId!, item => new StoredEventPublication(item.SequenceNumber, item.PublicationFingerprint!), StringComparer.Ordinal);
    }

    static Exception ClassifyIdentityAppendFailure(Exception error, bool writeIssued)
    {
        // 112 is WriteConflict, including server versions that omit the transient transaction label.
        var writeConflict = error switch
        {
            MongoCommandException command => command.Code == 112,
            MongoWriteException write => write.WriteError.Code == 112,
            MongoBulkWriteException<Event> bulk => bulk.WriteErrors.Any(item => item.Code == 112),
            _ => false
        };
        if (writeConflict && error is MongoException mongo && !mongo.HasErrorLabel("UnknownTransactionCommitResult"))
        {
            return new EventsNotCommitted(error);
        }
        return ClassifyAppendFailure(error, writeIssued);
    }

    async Task<AppendManyStorageFailure?> ClassifyIdentityCollision(EventToAppendToStorage[] batch)
    {
        var receipts = await TryGetPublications(batch.Where(item => item.Identity is not null).Select(item => item.Identity!.Id));
        if (receipts.Count > 0)
        {
            return new IdentitiesAlreadyDurable(receipts);
        }

        var numbers = batch.Select(item => item.SequenceNumber).ToArray();
        if (numbers.Distinct().Count() != numbers.Length || await _collection.Find(Builders<Event>.Filter.In(item => item.SequenceNumber, numbers)).AnyAsync().ConfigureAwait(false))
        {
            return new DuplicateEventSequenceNumber((await GetTailSequenceNumber()).Next());
        }
        return null;
    }
}
