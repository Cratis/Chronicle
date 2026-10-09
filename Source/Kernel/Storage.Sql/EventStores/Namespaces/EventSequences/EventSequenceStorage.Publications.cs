// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

public partial class EventSequenceStorage
{
    /// <inheritdoc/>
    public async Task<Option<EventPublicationReceipt>> TryGetPublication(EventPublication publication)
    {
        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(publication.Id));
        var existing = await scope.DbContext.Events.AsNoTracking()
            .SingleOrDefaultAsync(_ => _.PublicationIdentityHash == hash);
        if (existing is null)
        {
            return Option<EventPublicationReceipt>.None();
        }

        if (!string.Equals(existing.PublicationId, publication.Id, StringComparison.Ordinal) ||
            !string.Equals(existing.PublicationFingerprint, publication.Fingerprint, StringComparison.Ordinal))
        {
            throw new EventPublicationConflict();
        }

        return new EventPublicationReceipt(existing.SequenceNumber);
    }

    /// <inheritdoc/>
    public async Task<Result<EventPublicationReceipt, DuplicateEventSequenceNumber>> AppendPublication(EventPublication publication, EventToAppendToStorage @event)
    {
        if ((await TryGetPublication(publication)).TryGetValue(out var existing))
        {
            return existing;
        }

        try
        {
            await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);
            var entry = EventEntryConverter.ToEventEntry(
                @event.SequenceNumber,
                @event.EventSourceType,
                @event.EventSourceId,
                @event.EventStreamType,
                @event.EventStreamId,
                @event.EventType,
                @event.CorrelationId,
                @event.Causation,
                @event.CausedByChain,
                @event.Tags,
                TruncateToMicrosecond(@event.Occurred),
                @event.GenerationalContent,
                @event.ContentHashes,
                @event.Subject?.IsSet == true ? @event.Subject : null);
            entry.EventSource = @event.EventSource.IsSet ? @event.EventSource.Value : null;
            entry.PublicationIdentityHash = SHA256.HashData(Encoding.UTF8.GetBytes(publication.Id));
            entry.PublicationId = publication.Id;
            entry.PublicationFingerprint = publication.Fingerprint;
            scope.DbContext.Events.Add(entry);
            scope.DbContext.NamedTags.AddRange(@event.NamedTags.Select((tag, position) => NamedTagEntry.From(eventSequenceId.Value, @event.SequenceNumber.Value, position, tag)));
            await scope.DbContext.SaveChangesAsync();
            return new EventPublicationReceipt(@event.SequenceNumber);
        }
        catch (DbUpdateException exception) when (IsPublicationUniqueViolation(exception.InnerException))
        {
            if ((await TryGetPublication(publication)).TryGetValue(out var receipt))
            {
                return receipt;
            }

            await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);
            var sequenceNumber = @event.SequenceNumber.Value;
            if (!await scope.DbContext.Events.AnyAsync(_ => _.SequenceNumber == sequenceNumber))
            {
                throw;
            }

            return new DuplicateEventSequenceNumber(await GetNextAvailableSequenceNumber(scope));
        }
    }

    static bool IsPublicationUniqueViolation(Exception? exception) => exception switch
    {
        PostgresException postgres => postgres.SqlState == PostgresErrorCodes.UniqueViolation,
        SqlException sql => sql.Number is 2601 or 2627,
        SqliteException sqlite => sqlite.SqliteExtendedErrorCode is 1555 or 2067,
        _ => false
    };
}
