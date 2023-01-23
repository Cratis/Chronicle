// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Observation;

/// <summary>
/// Defines a system for working with observers.
/// </summary>
public interface IObservers
{
    /// <summary>
    /// Initialize the observers system.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Initialize();

    /// <summary>
    /// Get all the handlers registered.
    /// </summary>
    /// <returns>Collection of <see cref="ObserverHandler"/>.</returns>
    IEnumerable<ObserverHandler> Handlers { get; }
}
