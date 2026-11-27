// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Occurrences;
using Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

public partial class EventSequenceStorage
{
    /// <inheritdoc/>
    public Task<EffectReceipt?> GetEffectReceipt(NamespaceCommitContext context, string id) =>
        new NamespaceEventTransaction<EventSequenceDbContext>(OpenNamespaceEvents).Execute(context, (destination, cancellationToken) => ReadNamespaceEffectReceipt(destination, id, cancellationToken));

    /// <inheritdoc/>
    public Task<IEnumerable<EffectReceipt>> GetEffectReceipts(NamespaceCommitContext context, OccurrenceId occurrenceId) => GetEffectReceiptsForOccurrences(context, [occurrenceId]);

    /// <inheritdoc/>
    public Task<IEnumerable<EffectReceipt>> GetEffectReceiptsForOccurrences(NamespaceCommitContext context, IEnumerable<OccurrenceId> occurrenceIds) =>
        new NamespaceEventTransaction<EventSequenceDbContext>(OpenNamespaceEvents).Execute<IEnumerable<EffectReceipt>>(context, async (destination, cancellationToken) =>
        {
            var ids = occurrenceIds.Select(id => StorageIdentity.Hash(id.Value)).Distinct().ToArray();
            var entries = await destination.Set<EffectReceiptEntry>().AsNoTracking().Where(row => ids.Contains(row.OccurrenceId)).ToArrayAsync(cancellationToken);
            return entries.Select(entry => JsonSerializer.Deserialize<EffectReceipt>(entry.Data)!).ToArray();
        });

    /// <inheritdoc/>
    public async Task<EffectReceiptWriteResult> AppendManyWithEffectReceipt(NamespaceCommitContext context, IEnumerable<EventToAppendToStorage> events, EffectReceipt receipt)
    {
        var batch = events.ToArray();
        var transaction = new NamespaceEventTransaction<EventSequenceDbContext>(OpenNamespaceEvents);
        try
        {
            return await transaction.Execute(context, (destination, cancellationToken) => WriteNamespaceEffectReceipt(destination, batch, receipt, cancellationToken));
        }
        catch (Exception error) when (StorageWriteConflicts.IsTransactionConflict(error))
        {
            return EffectReceiptWriteResult.TransactionConflicted;
        }
        catch (Exception error) when (transaction.HasRemainingAttempts && StorageWriteConflicts.IsUnique(error))
        {
            var collision = await transaction.Execute<EffectReceiptWriteResult?>(context, async (destination, cancellationToken) =>
            {
                if (await ReadNamespaceEffectReceipt(destination, receipt.Id, cancellationToken) is not null) return EffectReceiptWriteResult.AlreadyRecorded;
                if (await ResolveNamespaceCollision(destination, batch, false, cancellationToken) is not null) return EffectReceiptWriteResult.DuplicateEventSequenceNumber;
                return null;
            });
            if (collision is not null) return collision.Value;
            throw;
        }
    }

    static async Task<EffectReceipt?> ReadNamespaceEffectReceipt(EventSequenceDbContext destination, string id, CancellationToken cancellationToken)
    {
        var hash = StorageIdentity.Hash(id);
        var entry = await destination.Set<EffectReceiptEntry>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == hash, cancellationToken);
        return entry is null ? null : JsonSerializer.Deserialize<EffectReceipt>(entry.Data);
    }

    async Task<EffectReceiptWriteResult> WriteNamespaceEffectReceipt(EventSequenceDbContext destination, EventToAppendToStorage[] batch, EffectReceipt receipt, CancellationToken cancellationToken)
    {
        if (await ReadNamespaceEffectReceipt(destination, receipt.Id, cancellationToken) is not null) return EffectReceiptWriteResult.AlreadyRecorded;
        EffectReceiptValidation.Validate(batch, receipt, eventSequenceId);
        AddNamespaceEvents(destination, batch, false);
        destination.Set<EffectReceiptEntry>().Add(new() { Id = StorageIdentity.Hash(receipt.Id), OccurrenceId = StorageIdentity.Hash(receipt.Lease.OccurrenceId.Value), Data = JsonSerializer.Serialize(receipt) });
        await destination.SaveChangesAsync(cancellationToken);
        return EffectReceiptWriteResult.Written;
    }
}
