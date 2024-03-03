// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Kernel.Storage.MongoDB.Sinks;

/// <summary>
/// Defines a context for a MongoDB collection used by <see cref="Sink"/>.
/// </summary>
public interface ISinkCollections
{
    /// <summary>
    /// Gets the <see cref="IMongoCollection{TDocument}"/> used by the <see cref="Sink"/>.
    /// </summary>
    /// <returns><see cref="IMongoCollection{T}"/> for the state.</returns>
    IMongoCollection<BsonDocument> GetCollection();

    /// <summary>
    /// Signals that a replay is about to begin.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task BeginReplay();

    /// <summary>
    /// Signals that a replay has ended.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task EndReplay();

    /// <summary>
    /// Prepare the sink for an initial run.
    /// </summary>
    /// <remarks>
    /// Typically the sink will clear out any existing data. This is to be able to guarantee
    /// the idempotency of the projection.
    /// </remarks>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task PrepareInitialRun();
}
