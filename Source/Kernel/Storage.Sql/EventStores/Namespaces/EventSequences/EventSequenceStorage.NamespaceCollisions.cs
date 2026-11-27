// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

public partial class EventSequenceStorage
{
    static async Task<IReadOnlyDictionary<string, StoredEventPublication>> ReadNamespacePublications(EventSequenceDbContext destination, IEnumerable<string> identities, CancellationToken cancellationToken)
    {
        var ids = identities.ToHashSet(StringComparer.Ordinal);
        var hashes = ids.Select(PublicationIdentityHash).ToArray();
        var entries = await destination.Events.AsNoTracking().Where(row => hashes.Contains(row.PublicationIdentityHash!)).WithoutHistory().ToArrayAsync(cancellationToken);
        return entries.Where(row => row.PublicationId is not null && ids.Contains(row.PublicationId))
            .ToDictionary(row => row.PublicationId!, row => new StoredEventPublication(row.SequenceNumber, row.PublicationFingerprint!), StringComparer.Ordinal);
    }

    static async Task<AppendManyStorageFailure?> ResolveNamespaceCollision(EventSequenceDbContext destination, EventToAppendToStorage[] batch, bool persistIdentities, CancellationToken cancellationToken)
    {
        if (persistIdentities)
        {
            var publications = await ReadNamespacePublications(destination, batch.Where(item => item.Identity is not null).Select(item => item.Identity!.Id), cancellationToken);
            if (publications.Count > 0) return new IdentitiesAlreadyDurable(publications);
        }
        var numbers = batch.Select(item => item.SequenceNumber.Value).ToArray();
        if (numbers.Distinct().Count() == numbers.Length && !await destination.Events.AnyAsync(row => numbers.Contains(row.SequenceNumber), cancellationToken)) return null;
        var highest = await destination.Events.AsNoTracking().OrderByDescending(row => row.SequenceNumber).Select(row => (ulong?)row.SequenceNumber).FirstOrDefaultAsync(cancellationToken);
        return new DuplicateEventSequenceNumber(highest is { } value ? new EventSequenceNumber(value).Next() : EventSequenceNumber.First);
    }
}
