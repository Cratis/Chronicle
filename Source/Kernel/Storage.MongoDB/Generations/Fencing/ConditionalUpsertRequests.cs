// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Tracks the bulk upserts whose filter holds a condition besides the primary key.
/// </summary>
/// <remarks>
/// When the primary key of such an upsert already exists and the condition does not hold, the server rejects the insert with a
/// duplicate key, which means the write was already applied. It can also mean a concurrent insert, so the request is tried once more
/// as a plain update before it is skipped. The driver's update model is sealed, so the requests are tracked beside it.
/// </remarks>
internal static class ConditionalUpsertRequests
{
    static readonly ConditionalWeakTable<WriteModel<BsonDocument>, object> _requests = [];

    /// <summary>
    /// Marks a request as a conditional upsert.
    /// </summary>
    /// <param name="request">The request.</param>
    public static void Mark(WriteModel<BsonDocument> request) => _requests.TryAdd(request, new object());

    /// <summary>
    /// Determines whether a request is a conditional upsert that is still an upsert.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>True if it is.</returns>
    public static bool IsConditionalUpsert(WriteModel<BsonDocument> request) =>
        request is UpdateOneModel<BsonDocument> { IsUpsert: true } && _requests.TryGetValue(request, out _);

    /// <summary>
    /// Turns a conditional upsert into a plain update, keeping the identity of the request.
    /// </summary>
    /// <param name="request">The request.</param>
    public static void RetryAsUpdate(WriteModel<BsonDocument> request)
    {
        if (request is UpdateOneModel<BsonDocument> update)
        {
            update.IsUpsert = false;
        }
    }
}
