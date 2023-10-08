// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Observation;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Defines a system for working with observer registrations for the Kernel.
/// </summary>
public interface IObservers
{
    /// <summary>
    /// Discover and register all observers discovered from the entry assembly.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Discover();

    /// <summary>
    /// Get all observers for specific event types.
    /// </summary>
    /// <param name="eventTypes">Collection of types representing events to get for.</param>
    /// <returns>Collection of <see cref="ObserverInformation"/> holding all information about the observers.</returns>
    Task<IEnumerable<ObserverInformation>> GetObserversForEventTypes(IEnumerable<Type> eventTypes);

    /// <summary>
    /// Get all observers.
    /// </summary>
    /// <returns>Collection of <see cref="ObserverInformation"/> holding all information about the observers.</returns>
    Task<IEnumerable<ObserverInformation>> GetAllObservers();
}
