// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Namespaces;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Occurrences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences;

public partial class EventSequenceStorage
{
    /// <inheritdoc/>
    public Task<EffectReceipt?> GetEffectReceipt(NamespaceCommitContext context, string id) =>
        new NamespaceEventTransaction(database).Execute(context, (session, cancellationToken) => ReadNamespaceEffectReceipt(session, id, cancellationToken));

    /// <inheritdoc/>
    public Task<IEnumerable<EffectReceipt>> GetEffectReceipts(NamespaceCommitContext context, OccurrenceId occurrenceId) => GetEffectReceiptsForOccurrences(context, [occurrenceId]);

    /// <inheritdoc/>
    public Task<IEnumerable<EffectReceipt>> GetEffectReceiptsForOccurrences(NamespaceCommitContext context, IEnumerable<OccurrenceId> occurrenceIds) =>
        new NamespaceEventTransaction(database).Execute<IEnumerable<EffectReceipt>>(context, async (session, cancellationToken) =>
        {
            var ids = occurrenceIds.Select(id => id.Value).Distinct().ToArray();
            var documents = await ReceiptCollection.Find(session, Builders<BsonDocument>.Filter.In("occurrenceId", ids)).ToListAsync(cancellationToken);
            return documents.Select(document => JsonSerializer.Deserialize<EffectReceipt>(document["data"].AsString)!).ToArray();
        });

    /// <inheritdoc/>
    public async Task<EffectReceiptWriteResult> AppendManyWithEffectReceipt(NamespaceCommitContext context, IEnumerable<EventToAppendToStorage> events, EffectReceipt receipt)
    {
        var batch = events.ToArray();
        var transaction = new NamespaceEventTransaction(database);
        try
        {
            return await transaction.Execute(context, (session, cancellationToken) => WriteNamespaceEffectReceipt(session, batch, receipt, cancellationToken));
        }
        catch (MongoException error) when (error.HasErrorLabel("TransientTransactionError") && !error.HasErrorLabel("UnknownTransactionCommitResult"))
        {
            return EffectReceiptWriteResult.TransactionConflicted;
        }
        catch (MongoException error) when (transaction.HasRemainingAttempts && !error.HasErrorLabel("UnknownTransactionCommitResult") && IsReceiptUniqueViolation(error))
        {
            var collision = await transaction.Execute<EffectReceiptWriteResult?>(context, async (session, cancellationToken) =>
            {
                if (await ReadNamespaceEffectReceipt(session, receipt.Id, cancellationToken) is not null) return EffectReceiptWriteResult.AlreadyRecorded;
                if (await ResolveNamespaceCollision(session, batch, false, cancellationToken) is not null) return EffectReceiptWriteResult.DuplicateEventSequenceNumber;
                return null;
            });
            if (collision is not null) return collision.Value;
            throw;
        }
    }

    async Task<EffectReceipt?> ReadNamespaceEffectReceipt(IClientSessionHandle session, string id, CancellationToken cancellationToken)
    {
        var document = await ReceiptCollection.Find(session, new BsonDocument("_id", id)).SingleOrDefaultAsync(cancellationToken);
        return document is null ? null : JsonSerializer.Deserialize<EffectReceipt>(document["data"].AsString);
    }

    async Task<EffectReceiptWriteResult> WriteNamespaceEffectReceipt(IClientSessionHandle session, EventToAppendToStorage[] batch, EffectReceipt receipt, CancellationToken cancellationToken)
    {
        if (await ReadNamespaceEffectReceipt(session, receipt.Id, cancellationToken) is not null) return EffectReceiptWriteResult.AlreadyRecorded;
        EffectReceiptValidation.Validate(batch, receipt, eventSequenceId);
        var documents = new List<Event>();
        foreach (var item in batch) documents.Add((await ReceiptEvent(item)) with { CompensationFor = item.CompensationFor });
        await ReceiptCollection.InsertOneAsync(session, new BsonDocument { { "_id", receipt.Id }, { "occurrenceId", receipt.Lease.OccurrenceId.Value }, { "data", JsonSerializer.Serialize(receipt) } }, cancellationToken: cancellationToken);
        if (documents.Count > 0) await _collection.InsertManyAsync(session, documents, cancellationToken: cancellationToken);
        return EffectReceiptWriteResult.Written;
    }
}
