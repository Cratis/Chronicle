// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences;

public partial class EventSequenceStorage : IEffectReceiptStorage
{
    IMongoCollection<BsonDocument> ReceiptCollection => _collection.Database.GetCollection<BsonDocument>("effect-receipts");

    /// <inheritdoc/>
    public async Task<EffectReceipt?> GetEffectReceipt(string id)
    {
        var document = await ReceiptCollection.Find(new BsonDocument("_id", id)).SingleOrDefaultAsync();
        return document is null ? null : JsonSerializer.Deserialize<EffectReceipt>(document["data"].AsString);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EffectReceipt>> GetEffectReceipts(OccurrenceId occurrenceId) => GetEffectReceiptsForOccurrences([occurrenceId]);

    /// <inheritdoc/>
    public async Task<IEnumerable<EffectReceipt>> GetEffectReceiptsForOccurrences(IEnumerable<OccurrenceId> occurrenceIds)
    {
        var ids = occurrenceIds.Select(id => id.Value).Distinct().ToArray();
        var documents = await ReceiptCollection.Find(Builders<BsonDocument>.Filter.In("occurrenceId", ids)).ToListAsync();

        return documents.Select(document => JsonSerializer.Deserialize<EffectReceipt>(document["data"].AsString)!).ToArray();
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
        var documents = new List<Event>();
        foreach (var item in batch)
        {
            documents.Add(await ReceiptEvent(item));
        }

        using var session = await database.Client.StartSessionAsync();
        try
        {
            session.StartTransaction();

            // Insert the receipt first to contend on its identity even when the event slots differ.
            await ReceiptCollection.InsertOneAsync(session, new BsonDocument { { "_id", receipt.Id }, { "occurrenceId", receipt.Lease.OccurrenceId.Value }, { "data", JsonSerializer.Serialize(receipt) } });
            if (documents.Count > 0)
            {
                await _collection.InsertManyAsync(session, documents);
            }

            await session.CommitTransactionAsync();
            return EffectReceiptWriteResult.Written;
        }
        catch (MongoException error) when (error.HasErrorLabel("UnknownTransactionCommitResult"))
        {
            // Never guess whether the commit succeeded; the next receipt lookup settles it.
            throw;
        }
        catch (MongoException error) when (error.HasErrorLabel("TransientTransactionError"))
        {
            await AbortReceiptTransaction(session);
            return EffectReceiptWriteResult.TransactionConflicted;
        }
        catch (MongoException error) when (IsReceiptUniqueViolation(error))
        {
            await AbortReceiptTransaction(session);
            if (await GetEffectReceipt(receipt.Id) is not null)
            {
                return EffectReceiptWriteResult.AlreadyRecorded;
            }

            var numbers = batch.Select(item => item.SequenceNumber).ToArray();
            if (numbers.Distinct().Count() != numbers.Length || await _collection.Find(Builders<Event>.Filter.In(item => item.SequenceNumber, numbers)).AnyAsync())
            {
                return EffectReceiptWriteResult.DuplicateEventSequenceNumber;
            }

            throw;
        }
        catch
        {
            await AbortReceiptTransaction(session);
            throw;
        }
    }

    static bool IsReceiptUniqueViolation(MongoException error) => error switch
    {
        MongoWriteException write => write.WriteError.Category == ServerErrorCategory.DuplicateKey,
        MongoBulkWriteException<Event> bulk => bulk.WriteErrors.Any(item => item.Category == ServerErrorCategory.DuplicateKey),
        _ => false
    };

    static async Task AbortReceiptTransaction(IClientSessionHandle session)
    {
        if (session.IsInTransaction)
        {
            await session.AbortTransactionAsync();
        }
    }

    async Task<Event> ReceiptEvent(EventToAppendToStorage item)
    {
        var content = new Dictionary<string, BsonDocument>();
        foreach (var (generation, value) in item.GenerationalContent)
        {
            var schema = await eventTypesStorage.GetFor(item.EventType.Id, generation);
            content[generation.ToString()] = SerializeContent(value, schema.Schema);
        }

        return new Event(item.SequenceNumber, item.CorrelationId, item.Causation.Select(StoredTimestamps.Normalize).ToArray(), item.CausedByChain.ToArray(), item.EventType.Id, StoredTimestamps.Normalize(item.Occurred), item.EventSourceType, item.EventSourceId, item.EventStreamType, item.EventStreamId, item.Tags.Select(tag => tag.Value).ToArray(), content, item.ContentHashes.ToDictionary(pair => pair.Key.ToString(), pair => pair.Value.Value), [], item.Subject?.IsSet == true ? item.Subject : null)
        {
            AppendedGeneration = item.EventType.Generation.Value,
            EventSource = item.EventSource.IsSet ? item.EventSource : null,
            NamedTags = item.NamedTags.Select(tag => new NamedTagDocument(tag.Name.Value, tag.Value)).ToArray()
        };
    }
}
