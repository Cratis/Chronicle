// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Benchmarks;

internal sealed class ProbePullMongoSource(IMongoDatabase database) : IPullPageSource
{
    internal const string Comment = "probe-4748-pull";
    internal const string EventType = "c0b93c8e-3f3f-4f3f-8f3f-3f3f3f3f3f3f";
    internal string? Sample { get; private set; }

    public async Task<PullEvent[]> Read(PullCursor? cursor, int page, CancellationToken cancellation)
    {
        var filter = new BsonDocument("_id", new BsonDocument(cursor is null ? "$gte" : "$gt", new Decimal128(cursor?.Position ?? 0)));
        using var rows = await database.GetCollection<BsonDocument>("event-log").FindAsync(filter, new FindOptions<BsonDocument>
        {
            Sort = new BsonDocument("_id", 1), Limit = page, BatchSize = page, Comment = Comment,
            Projection = new BsonDocument { { "_id", 1 }, { "eventSourceId", 1 }, { "type", 1 }, { "content", 1 } }
        }, cancellation);
        var documents = await rows.ToListAsync(cancellation);
        Sample ??= documents.FirstOrDefault()?.ToJson();
        return documents.Select(Decode).ToArray();
    }

    static PullEvent Decode(BsonDocument row)
    {
        var type = row["type"].AsString;
        ProbePayload? payload = null;
        if (type == EventType)
        {
            var content = row["content"]["1"].AsBsonDocument;
            if (!content.Contains("Timestamp") || !content.Contains("Value"))
                throw new InvalidOperationException($"Unsupported benchmark content BSON: {row.ToJson()}");
            var timestamp = content["Timestamp"];
            payload = new(content["Value"].ToInt32(), timestamp.IsBsonDateTime
                ? new DateTimeOffset(timestamp.AsBsonDateTime.ToUniversalTime())
                : DateTimeOffset.Parse(timestamp.AsString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
        }
        return new(checked((ulong)row["_id"].ToDecimal()), row["eventSourceId"].AsString, type, payload);
    }
}

internal sealed class ProbePullProgress(IMongoDatabase database, string stats) : IPullProgress
{
    internal const string ObserverId = "probe-4748-pull";
    long _revision;
    internal long Flushes { get; private set; }
    internal long CountDocuments { get; private set; }
    internal string? FinalDocument { get; private set; }
    internal string[] FinalCounts { get; private set; } = [];

    public async Task Save(PullPrefix prefix, IReadOnlyDictionary<string, long> counts, CancellationToken cancellation)
    {
        var durable = database.WithWriteConcern(WriteConcern.WMajority.With(journal: true));
        if (stats == "equivalent" && counts.Count > 0)
        {
            var wireComment = $"probe4748wire-{Guid.NewGuid():N}";
            var writes = counts.Select(pair => (WriteModel<BsonDocument>)new UpdateOneModel<BsonDocument>(
                new BsonDocument { { "_id", new BsonDocument { { "observerId", ObserverId }, { "partition", pair.Key } } }, { "$comment", wireComment } },
                new BsonDocument("$inc", new BsonDocument($"counts.{ProbePullMongoSource.EventType}", pair.Value))) { IsUpsert = true }).ToArray();
            await durable.GetCollection<BsonDocument>("pull-handled-counts").BulkWriteAsync(writes,
                new BulkWriteOptions { IsOrdered = false, Comment = ProbePullMongoSource.Comment }, cancellation);
            CountDocuments += writes.Length;
        }
        var filter = new BsonDocument { { "_id", ObserverId }, { "revision", _revision }, { "$comment", $"probe4748wire-{Guid.NewGuid():N}" } };
        var fields = new BsonDocument
        {
            { "w", prefix.W.HasValue ? new BsonDecimal128(new Decimal128(prefix.W.Value)) : BsonNull.Value },
            { "r", prefix.R.HasValue ? new BsonDecimal128(new Decimal128(prefix.R.Value)) : BsonNull.Value },
            { "handled", prefix.Handled }, { "handledPerType", new BsonDocument(ProbePullMongoSource.EventType, prefix.Handled) },
            { "updatedAt", DateTime.UtcNow }
        };
        var result = await durable.GetCollection<BsonDocument>("pull-progress").UpdateOneAsync(filter,
            new BsonDocument { { "$set", fields }, { "$inc", new BsonDocument("revision", 1) } },
            new UpdateOptions { IsUpsert = _revision == 0, Comment = ProbePullMongoSource.Comment }, cancellation);
        if (!result.IsAcknowledged || (result.MatchedCount != 1 && result.UpsertedId is null))
            throw new InvalidOperationException("Pull checkpoint revision write was not acknowledged/matched.");
        _revision++;
        Flushes++;
    }

    internal async Task<bool> AtTail(ulong tail, CancellationToken cancellation)
    {
        using var cursor = await database.GetCollection<BsonDocument>("pull-progress").FindAsync(new BsonDocument("_id", ObserverId),
            new FindOptions<BsonDocument> { Comment = "probe-4748-instrumentation" }, cancellation);
        var row = (await cursor.ToListAsync(cancellation)).SingleOrDefault();
        FinalDocument = row?.ToJson();
        return row is not null && !row["w"].IsBsonNull && row["w"].ToDecimal() >= tail;
    }

    internal async Task CaptureCounts(CancellationToken cancellation)
    {
        using var cursor = await database.GetCollection<BsonDocument>("pull-handled-counts").FindAsync(
            FilterDefinition<BsonDocument>.Empty, new FindOptions<BsonDocument> { Comment = "probe-4748-instrumentation" }, cancellation);
        FinalCounts = (await cursor.ToListAsync(cancellation)).Select(d => d.ToJson()).ToArray();
    }
}
