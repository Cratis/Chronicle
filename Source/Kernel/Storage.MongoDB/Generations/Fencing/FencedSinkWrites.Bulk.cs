// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

public sealed partial class FencedSinkWrites
{
    /// <inheritdoc/>
    public async Task BulkWrite(IMongoCollection<BsonDocument> collection, IReadOnlyList<WriteModel<BsonDocument>> requests, CancellationToken cancellationToken)
    {
        if (_splitting.Value)
        {
            SinkBatchDocuments.RequireRetrySafe(collection, requests);
        }
        if (_session.Value is not null)
        {
            await _operation.WaitAsync(cancellationToken);
            try
            {
                foreach (var request in requests)
                {
                    await _documents.Value!.Reserve(collection, _session.Value, request, cancellationToken);
                }
                await collection.BulkWriteAsync(_session.Value, requests, cancellationToken: cancellationToken);
            }
            finally
            {
                _operation.Release();
            }
            return;
        }

        try
        {
            await Run(
                async _ =>
                {
                    await BulkWrite(collection, requests, cancellationToken);
                    return true;
                },
                cancellationToken);
        }
        catch (SinkBatchLimitExceeded)
        {
            SinkBatchDocuments.RequireRetrySafe(collection, requests);
            foreach (var chunk in requests.Chunk(_cap))
            {
                try
                {
                    await BulkWrite(collection, chunk, cancellationToken);
                }
                catch (MongoBulkWriteException<BsonDocument> error)
                {
                    // A chunk-local error index must never be used to resume the original bulk buffer.
                    throw new InvalidOperationException("A capped fenced bulk write failed after partial progress; retry the whole changeset.", error);
                }
            }
        }
        catch (MongoBulkWriteException<BsonDocument> error) when (error.WriteErrors.Count > 0)
        {
            var failedAt = error.WriteErrors.Min(writeError => writeError.Index);
            if (failedAt > 0)
            {
                await BulkWrite(collection, requests.Take(failedAt).ToList(), cancellationToken);
            }
            throw;
        }
    }
}
