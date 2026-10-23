// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;
using Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

public partial class EventSequenceStorage : IEffectReceiptStorage
{
    /// <inheritdoc/>
    public async Task<EffectReceipt?> GetEffectReceipt(string id)
    {
        await using var namespaceScope = await database.Namespace(eventStore, @namespace);
        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);
        var hash = StorageIdentity.Hash(id);
        var entry = await scope.DbContext.Set<EffectReceiptEntry>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == hash);
        return entry is null ? null : JsonSerializer.Deserialize<EffectReceipt>(entry.Data);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EffectReceipt>> GetEffectReceipts(OccurrenceId occurrenceId) => GetEffectReceiptsForOccurrences([occurrenceId]);

    /// <inheritdoc/>
    public async Task<IEnumerable<EffectReceipt>> GetEffectReceiptsForOccurrences(IEnumerable<OccurrenceId> occurrenceIds)
    {
        var ids = occurrenceIds.Select(id => StorageIdentity.Hash(id.Value)).Distinct().ToArray();
        await using var namespaceScope = await database.Namespace(eventStore, @namespace);
        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);
        var entries = await scope.DbContext.Set<EffectReceiptEntry>().AsNoTracking().Where(row => ids.Contains(row.OccurrenceId)).ToArrayAsync();

        return entries.Select(entry => JsonSerializer.Deserialize<EffectReceipt>(entry.Data)!).ToArray();
    }

    /// <inheritdoc/>
    public async Task<EffectReceiptWriteResult> AppendManyWithEffectReceipt(IEnumerable<EventToAppendToStorage> events, EffectReceipt receipt)
    {
        if (await GetEffectReceipt(receipt.Id) is not null)
        {
            return EffectReceiptWriteResult.AlreadyRecorded;
        }

        var batch = events.ToArray();
        EffectReceiptValidation.Validate(batch, receipt, eventSequenceId);
        try
        {
            await using var namespaceScope = await database.Namespace(eventStore, @namespace);
            await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);
            foreach (var item in batch)
            {
                var entry = EventEntryConverter.ToEventEntry(item.SequenceNumber, item.EventSourceType, item.EventSourceId, item.EventStreamType, item.EventStreamId, item.EventType, item.CorrelationId, item.Causation, item.CausedByChain, item.Tags, TruncateToMicrosecond(item.Occurred), item.GenerationalContent, item.ContentHashes, item.Subject?.IsSet == true ? item.Subject : null);
                entry.EventSource = item.EventSource.IsSet ? item.EventSource.Value : null;
                scope.DbContext.Events.Add(entry);
                scope.DbContext.NamedTags.AddRange(item.NamedTags.Select((tag, position) => NamedTagEntry.From(eventSequenceId.Value, item.SequenceNumber.Value, position, tag)));
            }

            scope.DbContext.Set<EffectReceiptEntry>().Add(new EffectReceiptEntry { Id = StorageIdentity.Hash(receipt.Id), OccurrenceId = StorageIdentity.Hash(receipt.Lease.OccurrenceId.Value), Data = JsonSerializer.Serialize(receipt) });

            // Includes the receipt even for a zero-event validate-only batch.
            await scope.DbContext.SaveChangesAsync();
            return EffectReceiptWriteResult.Written;
        }
        catch (Exception error) when (StorageWriteConflicts.IsUnique(error))
        {
            // The failed SaveChanges transaction has rolled back before either lookup.
            if (await GetEffectReceipt(receipt.Id) is not null)
            {
                SqlStorageTelemetry.RecoveredCollision("effect-receipt");
                return EffectReceiptWriteResult.AlreadyRecorded;
            }

            await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);
            var numbers = batch.Select(item => item.SequenceNumber.Value).ToArray();
            if (numbers.Distinct().Count() != numbers.Length || await scope.DbContext.Events.AnyAsync(row => numbers.Contains(row.SequenceNumber)))
            {
                return EffectReceiptWriteResult.DuplicateEventSequenceNumber;
            }

            throw;
        }
        catch (Exception error) when (StorageWriteConflicts.IsTransactionConflict(error))
        {
            return EffectReceiptWriteResult.TransactionConflicted;
        }
    }
}
