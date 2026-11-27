// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;
using Cratis.Monads;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

public partial class EventSequenceStorage
{
    /// <inheritdoc/>
    public Task<Option<EventPublicationReceipt>> TryGetPublication(NamespaceCommitContext context, EventPublication publication) =>
        new NamespaceEventTransaction<EventSequenceDbContext>(OpenNamespaceEvents).Execute(context, (destination, cancellationToken) => ReadNamespacePublication(destination, publication, cancellationToken));

    /// <inheritdoc/>
    public Task<IReadOnlyDictionary<string, StoredEventPublication>> TryGetPublications(NamespaceCommitContext context, IEnumerable<string> identities) =>
        new NamespaceEventTransaction<EventSequenceDbContext>(OpenNamespaceEvents).Execute(context, (destination, cancellationToken) => ReadNamespacePublications(destination, identities, cancellationToken));

    /// <inheritdoc/>
    public async Task<Result<EventPublicationReceipt, DuplicateEventSequenceNumber>> AppendPublication(NamespaceCommitContext context, EventPublication publication, EventToAppendToStorage @event)
    {
        var transaction = new NamespaceEventTransaction<EventSequenceDbContext>(OpenNamespaceEvents);
        try
        {
            return await transaction.Execute<Result<EventPublicationReceipt, DuplicateEventSequenceNumber>>(context, async (destination, cancellationToken) =>
            {
                if ((await ReadNamespacePublication(destination, publication, cancellationToken)).TryGetValue(out var existing)) return existing;
                AddNamespaceEvents(destination, [@event with { Identity = publication }], true);
                await destination.SaveChangesAsync(cancellationToken);
                return new EventPublicationReceipt(@event.SequenceNumber);
            });
        }
        catch (Exception error) when (transaction.HasRemainingAttempts && StorageWriteConflicts.IsUnique(error))
        {
            var collision = await transaction.Execute<Result<EventPublicationReceipt, DuplicateEventSequenceNumber>?>(context, async (destination, cancellationToken) =>
            {
                if ((await ReadNamespacePublication(destination, publication, cancellationToken)).TryGetValue(out var existing)) return Result<EventPublicationReceipt, DuplicateEventSequenceNumber>.Success(existing);
                if (await ResolveNamespaceCollision(destination, [@event], false, cancellationToken) is { IsT0: true } duplicate) return Result<EventPublicationReceipt, DuplicateEventSequenceNumber>.Failed(duplicate.AsT0);
                return null;
            });
            if (collision is not null) return collision;
            throw;
        }
    }

    static async Task<Option<EventPublicationReceipt>> ReadNamespacePublication(EventSequenceDbContext destination, EventPublication publication, CancellationToken cancellationToken)
    {
        var hash = PublicationIdentityHash(publication.Id);
        var entry = await destination.Events.AsNoTracking().SingleOrDefaultAsync(row => row.PublicationIdentityHash == hash, cancellationToken);
        if (entry is null) return Option<EventPublicationReceipt>.None();
        if (!string.Equals(entry.PublicationId, publication.Id, StringComparison.Ordinal) || !string.Equals(entry.PublicationFingerprint, publication.Fingerprint, StringComparison.Ordinal)) throw new EventPublicationConflict();
        return new EventPublicationReceipt(entry.SequenceNumber);
    }
}
