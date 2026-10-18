// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

public partial class EventSequenceStorage
{
    /// <inheritdoc/>
    public Task<Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>> AppendManyWithIdentities(IEnumerable<EventToAppendToStorage> events) => AppendBatch(events, true);

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, StoredEventPublication>> TryGetPublications(IEnumerable<string> identities)
    {
        var ids = identities.ToHashSet(StringComparer.Ordinal);
        if (ids.Count == 0)
        {
            return new Dictionary<string, StoredEventPublication>(StringComparer.Ordinal);
        }
        var hashes = ids.Select(PublicationIdentityHash).ToArray();
        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);
        var entries = await scope.DbContext.Events.AsNoTracking()
            .Where(row => hashes.Contains(row.PublicationIdentityHash!)).ToArrayAsync();
        return entries.Where(row => row.PublicationId is not null && ids.Contains(row.PublicationId))
            .ToDictionary(row => row.PublicationId!, row => new StoredEventPublication(row.SequenceNumber, row.PublicationFingerprint!), StringComparer.Ordinal);
    }

    static byte[] PublicationIdentityHash(string identity) => SHA256.HashData(Encoding.UTF8.GetBytes(identity));

    async Task<AppendManyStorageFailure?> ClassifyIdentityCollision(EventToAppendToStorage[] batch)
    {
        var receipts = await TryGetPublications(batch.Where(item => item.Identity is not null).Select(item => item.Identity!.Id));
        if (receipts.Count > 0)
        {
            return new IdentitiesAlreadyDurable(receipts);
        }

        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);
        var numbers = batch.Select(item => item.SequenceNumber.Value).ToArray();
        if (numbers.Distinct().Count() != numbers.Length || await scope.DbContext.Events.AnyAsync(row => numbers.Contains(row.SequenceNumber)))
        {
            return new DuplicateEventSequenceNumber(await GetNextAvailableSequenceNumber(scope));
        }
        return null;
    }
}
