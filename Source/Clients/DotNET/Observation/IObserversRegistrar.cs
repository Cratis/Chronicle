// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Observation;

/// <summary>
/// Defines a system for working with observers.
/// </summary>
public interface IObserversRegistrar
{
    /// <summary>
    /// Initialize the observers system.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Initialize();

    /// <summary>
    /// Get all registered handlers.
    /// </summary>
    /// <returns>Collection of <see cref="ObserverHandler"/>.</returns>
    IEnumerable<ObserverHandler> GetAll();

    /// <summary>
    /// Gets a specific handler by its id.
    /// </summary>
    /// <param name="observerId"><see cref="ObserverId"/> to get for.</param>
    /// <returns><see cref="ObserverHandler"/>.</returns>
    ObserverHandler GetById(ObserverId observerId);

    /// <summary>
    /// Gets a specific handler by its id.
    /// </summary>
    /// <param name="observerType">The <see cref="ObserverType"/> to get for.</param>
    /// <returns><see cref="ObserverHandler"/>.</returns>
    ObserverHandler GetByType(Type observerType);

    /// <summary>
    /// Get the CLR type for a specific observer.
    /// </summary>
    /// <param name="observerId"><see cref="ObserverId"/> to get for.</param>
    /// <returns>The type.</returns>
    Type GetClrType(ObserverId observerId);
}
