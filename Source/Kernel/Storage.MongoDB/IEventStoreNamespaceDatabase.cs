// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Observation;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Defines the database for accessing a specific namespace of the event store.
/// </summary>
public interface IEventStoreNamespaceDatabase
{
    /// <summary>
    /// Gets the <see cref="IMongoClient"/> for this database.
    /// </summary>
    IMongoClient Client { get; }

    /// <summary>
    /// Get a collection - optionally by its name. If no name is given, it will go by convention from the type name.
    /// </summary>
    /// <param name="name">Optional name of the collection.</param>
    /// <typeparam name="T">Type to get collection for.</typeparam>
    /// <returns>A <see cref="IMongoCollection{T}"/> instance.</returns>
    IMongoCollection<T> GetCollection<T>(string? name = default);

    /// <summary>
    /// Get the <see cref="IMongoCollection{T}"/> for an event sequence based on identifier.
    /// </summary>
    /// <param name="eventSequenceId"><see cref="EventSequenceId"/> identifier.</param>
    /// <returns>The collection instance.</returns>
    IMongoCollection<Event> GetEventSequenceCollectionFor(EventSequenceId eventSequenceId);

    /// <summary>
    /// Get the <see cref="IMongoCollection{T}"/> for an event sequence based on identifier as <see cref="BsonDocument"/>.
    /// </summary>
    /// <param name="eventSequenceId"><see cref="EventSequenceId"/> identifier.</param>
    /// <returns>The collection instance.</returns>
    IMongoCollection<BsonDocument> GetEventSequenceCollectionAsBsonFor(EventSequenceId eventSequenceId);

    /// <summary>
    /// Get the <see cref="IMongoCollection{T}"/> for the observer state.
    /// </summary>
    /// <returns>The collection instance.</returns>
    IMongoCollection<ObserverState> GetObserverStateCollection();

    /// <summary>
    /// Ensures the indexes for an event sequence collection exist, pruning obsolete ones. Runs at most once per
    /// event sequence for the lifetime of the database instance.
    /// </summary>
    /// <param name="eventSequenceId"><see cref="EventSequenceId"/> to ensure indexes for.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task EnsureIndexesForEventSequence(EventSequenceId eventSequenceId);

    /// <summary>
    /// Checks whether the database backing this namespace has ever been materialized.
    /// </summary>
    /// <returns>True if at least one collection exists in the database; false if MongoDB has never actually
    /// created it.</returns>
    /// <remarks>
    /// MongoDB creates a database lazily - connecting to it, or even getting a collection reference from it, does
    /// not create anything on the server. Only a write (an insert, an upsert, or an index creation) does. Listing
    /// the collection names is itself a read and never creates the database, which is what makes it a safe check
    /// for "does this namespace have anything in it yet" without the check itself being the very thing that
    /// materializes it.
    /// </remarks>
    Task<bool> HasAnyCollections();
}
