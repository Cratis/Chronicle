// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Namespaces;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Monads;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences;

public partial class EventSequenceStorage
{
    /// <inheritdoc/>
    public Task<Option<EventPublicationReceipt>> TryGetPublication(NamespaceCommitContext context, EventPublication publication) =>
        new NamespaceEventTransaction(database).Execute(context, (session, cancellationToken) => ReadNamespacePublication(session, publication, cancellationToken));

    /// <inheritdoc/>
    public Task<IReadOnlyDictionary<string, StoredEventPublication>> TryGetPublications(NamespaceCommitContext context, IEnumerable<string> identities) =>
        new NamespaceEventTransaction(database).Execute(context, (session, cancellationToken) => ReadNamespacePublications(session, identities, cancellationToken));

    /// <inheritdoc/>
    public async Task<Result<EventPublicationReceipt, DuplicateEventSequenceNumber>> AppendPublication(NamespaceCommitContext context, EventPublication publication, EventToAppendToStorage @event)
    {
        var transaction = new NamespaceEventTransaction(database);
        try
        {
            return await transaction.Execute<Result<EventPublicationReceipt, DuplicateEventSequenceNumber>>(context, async (session, cancellationToken) =>
            {
                if ((await ReadNamespacePublication(session, publication, cancellationToken)).TryGetValue(out var existing)) return existing;
                var document = (await ReceiptEvent(@event)) with
                {
                    PublicationId = publication.Id,
                    PublicationFingerprint = publication.Fingerprint,
                    CompensationFor = @event.CompensationFor
                };
                await _collection.InsertOneAsync(session, document, cancellationToken: cancellationToken);
                return new EventPublicationReceipt(@event.SequenceNumber);
            });
        }
        catch (MongoException error) when (transaction.HasRemainingAttempts && !error.HasErrorLabel("UnknownTransactionCommitResult") && IsReceiptUniqueViolation(error))
        {
            var collision = await transaction.Execute(context, async (session, cancellationToken) =>
            {
                var receipt = await ReadNamespacePublication(session, publication, cancellationToken);
                var slot = receipt.TryGetValue(out _) ? null : await ResolveNamespaceCollision(session, [@event], false, cancellationToken);
                return (Receipt: receipt, Slot: slot);
            });
            if (collision.Receipt.TryGetValue(out var receipt)) return receipt;
            if (collision.Slot is { IsT0: true }) return collision.Slot.AsT0;
            throw;
        }
    }

    async Task<Option<EventPublicationReceipt>> ReadNamespacePublication(IClientSessionHandle session, EventPublication publication, CancellationToken cancellationToken)
    {
        var existing = await _collection.Find(session, Builders<Event>.Filter.Eq(item => item.PublicationId, publication.Id)).SingleOrDefaultAsync(cancellationToken);
        if (existing is null) return Option<EventPublicationReceipt>.None();
        if (!string.Equals(existing.PublicationFingerprint, publication.Fingerprint, StringComparison.Ordinal)) throw new EventPublicationConflict();
        return new EventPublicationReceipt(existing.SequenceNumber);
    }

    async Task<IReadOnlyDictionary<string, StoredEventPublication>> ReadNamespacePublications(IClientSessionHandle session, IEnumerable<string> identities, CancellationToken cancellationToken)
    {
        var ids = identities.Distinct(StringComparer.Ordinal).ToArray();
        var documents = await _collection.Find(session, Builders<Event>.Filter.In(item => item.PublicationId, ids)).ToListAsync(cancellationToken);
        return documents.ToDictionary(item => item.PublicationId!, item => new StoredEventPublication(item.SequenceNumber, item.PublicationFingerprint!), StringComparer.Ordinal);
    }
}
