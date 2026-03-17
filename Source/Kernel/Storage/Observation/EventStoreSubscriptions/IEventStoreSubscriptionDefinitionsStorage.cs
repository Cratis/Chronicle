// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;

namespace Cratis.Chronicle.Storage.Observation.EventStoreSubscriptions;

/// <summary>
/// Defines a system for working with <see cref="EventStoreSubscriptionDefinition"/> storage.
/// </summary>
public interface IEventStoreSubscriptionDefinitionsStorage
{
    /// <summary>
    /// Get all <see cref="EventStoreSubscriptionDefinition">definitions</see> registered.
    /// </summary>
    /// <returns>A collection of <see cref="EventStoreSubscriptionDefinition"/>.</returns>
    Task<IEnumerable<EventStoreSubscriptionDefinition>> GetAll();

    /// <summary>
    /// Check if an <see cref="EventStoreSubscriptionDefinition"/> exists by its <see cref="EventStoreSubscriptionId"/>.
    /// </summary>
    /// <param name="id"><see cref="EventStoreSubscriptionId"/> to check for.</param>
    /// <returns>True if it exists, false if not.</returns>
    Task<bool> Has(EventStoreSubscriptionId id);

    /// <summary>
    /// Get a specific <see cref="EventStoreSubscriptionDefinition"/> by its <see cref="EventStoreSubscriptionId"/>.
    /// </summary>
    /// <param name="id"><see cref="EventStoreSubscriptionId"/> to get for.</param>
    /// <returns><see cref="EventStoreSubscriptionDefinition"/>.</returns>
    Task<EventStoreSubscriptionDefinition?> Get(EventStoreSubscriptionId id);

    /// <summary>
    /// Delete an <see cref="EventStoreSubscriptionDefinition"/> by its <see cref="EventStoreSubscriptionId"/>.
    /// </summary>
    /// <param name="id"><see cref="EventStoreSubscriptionId"/> of the <see cref="EventStoreSubscriptionDefinition"/> to delete.</param>
    /// <returns>Awaitable task.</returns>
    Task Delete(EventStoreSubscriptionId id);

    /// <summary>
    /// Save an <see cref="EventStoreSubscriptionDefinition"/>.
    /// </summary>
    /// <param name="definition">Definition to save.</param>
    /// <returns>Async task.</returns>
    Task Save(EventStoreSubscriptionDefinition definition);
}
