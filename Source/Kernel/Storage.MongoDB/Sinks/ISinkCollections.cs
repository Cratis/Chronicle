// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

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
    /// <param name="context">The <see cref="ReplayContext"/> for the replay.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task BeginReplay(Chronicle.Storage.Sinks.ReplayContext context);

    /// <summary>
    /// Signals that a replay is about to resume.
    /// </summary>
    /// <param name="context">The <see cref="ReplayContext"/> for the replay.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ResumeReplay(Chronicle.Storage.Sinks.ReplayContext context);

    /// <summary>
    /// Signals that a replay has ended.
    /// </summary>
    /// <param name="context">The <see cref="ReplayContext"/> for the replay.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task EndReplay(Chronicle.Storage.Sinks.ReplayContext context);

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
