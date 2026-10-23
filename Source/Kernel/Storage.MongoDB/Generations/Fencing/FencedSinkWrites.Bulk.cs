// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

public sealed partial class FencedSinkWrites
{
    /// <inheritdoc/>
    public async Task BulkWrite(IMongoCollection<BsonDocument> collection, IReadOnlyList<WriteModel<BsonDocument>> requests, CancellationToken cancellationToken, IReadOnlySet<WriteModel<BsonDocument>>? guardedUpserts = null)
    {
        if (_splitting.Value)
        {
            SinkBatchDocuments.RequireRetrySafe(collection, requests);
        }
        if (_session.Value is not null)
        {
            await BulkWriteInSession(collection, requests, cancellationToken);
            return;
        }

        try
        {
            await Run(
                async _ =>
                {
                    await BulkWrite(collection, requests, cancellationToken, guardedUpserts);
                    return true;
                },
                cancellationToken);
        }
        catch (SinkBatchLimitExceeded)
        {
            await WriteCappedBulk(collection, requests, guardedUpserts, cancellationToken);
        }
        catch (MongoBulkWriteException<BsonDocument> error) when (error.WriteErrors.Count > 0)
        {
            var failedAt = error.WriteErrors.Min(writeError => writeError.Index);
            if (failedAt > 0)
            {
                await BulkWrite(collection, requests.Take(failedAt).ToList(), cancellationToken, guardedUpserts);
            }
            if (!UnfencedSinkWrites.IsRejectedGuardedUpsert(error, requests, guardedUpserts))
            {
                throw;
            }
            var remaining = requests.Skip(failedAt + 1).ToArray();
            if (remaining.Length > 0)
            {
                await BulkWrite(collection, remaining, cancellationToken, guardedUpserts);
            }
        }
    }

    async Task BulkWriteInSession(IMongoCollection<BsonDocument> collection, IReadOnlyList<WriteModel<BsonDocument>> requests, CancellationToken cancellationToken)
    {
        await _operation.WaitAsync(cancellationToken);
        try
        {
            foreach (var request in requests)
            {
                await _documents.Value!.Reserve(collection, _session.Value!, request, cancellationToken);
            }
            await collection.BulkWriteAsync(_session.Value!, requests, cancellationToken: cancellationToken);
        }
        finally
        {
            _operation.Release();
        }
    }

    async Task WriteCappedBulk(IMongoCollection<BsonDocument> collection, IReadOnlyList<WriteModel<BsonDocument>> requests, IReadOnlySet<WriteModel<BsonDocument>>? guardedUpserts, CancellationToken cancellationToken)
    {
        SinkBatchDocuments.RequireRetrySafe(collection, requests);
        foreach (var chunk in requests.Chunk(_cap))
        {
            try
            {
                await BulkWrite(collection, chunk, cancellationToken, guardedUpserts);
            }
            catch (MongoBulkWriteException<BsonDocument> error)
            {
                // A chunk-local error index must never be used to resume the original bulk buffer.
                throw new InvalidOperationException("A capped fenced bulk write failed after partial progress; retry the whole changeset.", error);
            }
        }
    }
}
