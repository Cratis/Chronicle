// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Defines a system for working with <see cref="ObserverDefinition"/>.
/// </summary>
public interface IObserverDefinitionsStorage
{
    /// <summary>
    /// Get all <see cref="ObserverDefinition">definitions</see> registered.
    /// </summary>
    /// <returns>A collection of <see cref="ObserverDefinition"/>.</returns>
    Task<IEnumerable<ObserverDefinition>> GetAll();

    /// <summary>
    /// Check if a <see cref="ObserverDefinition"/> exists by its <see cref="ObserverId"/>.
    /// </summary>
    /// <param name="id"><see cref="ObserverId"/> to check for.</param>
    /// <returns>True if it exists, false if not.</returns>
    Task<bool> Has(ObserverId id);

    /// <summary>
    /// Get a specific <see cref="ObserverDefinition"/> by its <see cref="ObserverId"/>.
    /// </summary>
    /// <param name="id"><see cref="ObserverId"/> to get for.</param>
    /// <returns><see cref="ObserverDefinition"/>.</returns>
    Task<ObserverDefinition> Get(ObserverId id);

    /// <summary>
    /// Delete a <see cref="ObserverDefinition"/> by its <see cref="ObserverId"/>.
    /// </summary>
    /// <param name="id"><see cref="ObserverId"/> of the <see cref="ObserverDefinition"/> to delete.</param>
    /// <returns>Awaitable task.</returns>
    Task Delete(ObserverId id);

    /// <summary>
    /// Save a <see cref="ObserverDefinition"/>.
    /// </summary>
    /// <param name="definition">Definition to save.</param>
    /// <returns>Async task.</returns>
    Task Save(ObserverDefinition definition);

    /// <summary>
    /// Get all observers for specific event types.
    /// </summary>
    /// <param name="eventTypes">Collection of <see cref="EventType"/> to get for.</param>
    /// <returns>Collection of <see cref="ObserverDefinition"/> holding all information about the observers.</returns>
    Task<IEnumerable<ObserverDefinition>> GetForEventTypes(IEnumerable<EventType> eventTypes);
}
