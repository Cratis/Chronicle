// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Observation;

/// <summary>
/// Defines a system for working with observer registrations for the Kernel.
/// </summary>
public interface IObservers
{
    /// <summary>
    /// Discover all observers from the entry assembly and dependencies.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Discover();

    /// <summary>
    /// Register all observers with the Cratis Kernel.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Register();

    /// <summary>
    /// Gets a specific handler by its <see cref="ObserverId"/>.
    /// </summary>
    /// <param name="id"><see cref="ObserverId"/> to get for.</param>
    /// <returns><see cref="ObserverHandler"/> instance.</returns>
    ObserverHandler GetHandlerById(ObserverId id);
}
