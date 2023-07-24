// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Sinks;

/// <summary>
/// Defines a context for a MongoDB collection used by <see cref="MongoDBSink"/>.
/// </summary>
public interface IMongoDBSinkCollections
{
    /// <summary>
    /// Gets the <see cref="IMongoCollection{TDocument}"/> used by the <see cref="MongoDBSink"/>.
    /// </summary>
    /// <param name="isReplaying">Whether or not to get the collection for replay or not.</param>
    /// <returns><see cref="IMongoCollection{T}"/> for the state.</returns>
    IMongoCollection<BsonDocument> GetCollection(bool isReplaying);

    /// <summary>
    /// Signals that a replay is about to begin.
    /// </summary>
    Task BeginReplay();

    /// <summary>
    /// Signals that a replay has ended.
    /// </summary>
    Task EndReplay();

    /// <summary>
    /// Prepare the sink for an initial run.
    /// </summary>
    /// <remarks>
    /// Typically the sink will clear out any existing data. This is to be able to guarantee
    /// the idempotency of the projection.
    /// </remarks>
    /// <param name="isReplaying">Whether or not we're in replay mode.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task PrepareInitialRun(bool isReplaying);
}
