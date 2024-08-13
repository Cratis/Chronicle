// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Defines the storage for the cluster level.
/// </summary>
public interface IStorage
{
    /// <summary>
    /// Get all the <see cref="EventStoreName">event stores</see> available.
    /// </summary>
    /// <returns>Collection of <see cref="EventStoreName"/>.</returns>
    Task<IEnumerable<EventStoreName>> GetEventStores();

    /// <summary>
    /// Observes all the <see cref="EventStoreName">event stores</see> available.
    /// </summary>
    /// <returns>Collection of <see cref="EventStoreName"/>.</returns>
    ISubject<IEnumerable<EventStoreName>> ObserveEventStores();

    /// <summary>
    /// Get the <see cref="IEventStoreStorage"/> for a specific <see cref="EventStoreName"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> to get.</param>
    /// <returns>The <see cref="IEventStoreStorage"/> instance.</returns>
    IEventStoreStorage GetEventStore(EventStoreName eventStore);
}
