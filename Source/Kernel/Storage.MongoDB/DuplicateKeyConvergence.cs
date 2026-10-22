// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Helpers that let startup-time writes converge when several nodes race to create the same document (E11000).
/// </summary>
/// <remarks>
/// Only use these for idempotent "create if missing" writes. Inserts where a duplicate is a meaningful outcome
/// (event appends, constraints, receipts, idempotency keys) must keep surfacing the duplicate key error.
/// </remarks>
internal static class DuplicateKeyConvergence
{
    /// <summary>
    /// Check whether a write exception is a duplicate key violation.
    /// </summary>
    /// <param name="exception">The <see cref="MongoWriteException"/> to check.</param>
    /// <returns>True if it is a duplicate key violation.</returns>
    public static bool IsDuplicateKey(this MongoWriteException exception) =>
        exception.WriteError?.Category == ServerErrorCategory.DuplicateKey;

    /// <summary>
    /// Insert a document once. If another writer already inserted the same key, the insert converges when the
    /// stored document is equivalent to the one being inserted; a different winner is never overwritten and the
    /// duplicate key error is rethrown.
    /// </summary>
    /// <typeparam name="TDocument">Type of document.</typeparam>
    /// <param name="collection">The <see cref="IMongoCollection{TDocument}"/> to insert into.</param>
    /// <param name="document">The document to insert.</param>
    /// <param name="isSameKey">Expression matching the stored document that holds the same key.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task InsertOnce<TDocument>(
        this IMongoCollection<TDocument> collection,
        TDocument document,
        Expression<Func<TDocument, bool>> isSameKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.IsDuplicateKey())
        {
            using var cursor = await collection.FindAsync(isSameKey, cancellationToken: cancellationToken);
            var winner = await cursor.FirstOrDefaultAsync(cancellationToken);
            if (winner is null || !winner.ToBsonDocument().Equals(document.ToBsonDocument()))
            {
                throw;
            }
        }
    }

    /// <summary>
    /// Run an upsert, retrying it once when it loses a race against a concurrent upsert of the same key.
    /// </summary>
    /// <param name="write">The upsert to run.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task UpsertWithRetry(Func<Task> write)
    {
        try
        {
            await write();
        }
        catch (MongoWriteException exception) when (exception.IsDuplicateKey())
        {
            await write();
        }
    }
}
