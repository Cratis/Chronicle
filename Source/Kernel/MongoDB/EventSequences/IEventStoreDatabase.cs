// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Aksio.Cratis.Events.Store.MongoDB;

/// <summary>
/// Defines the database used by the event store.
/// </summary>
public interface IEventStoreDatabase
{
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
}
