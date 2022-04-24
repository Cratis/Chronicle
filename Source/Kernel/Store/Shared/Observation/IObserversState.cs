// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Aksio.Cratis.Events.Store.Observation;

/// <summary>
/// Defines a system for working with <see cref="ObserverState"/>.
/// </summary>
public interface IObserversState
{
    /// <summary>
    /// Gets an <see cref="IObservable{T}"/> for all instances of <see cref="ObserverState"/>.
    /// </summary>
    IObservable<IEnumerable<ObserverState>> All { get; }
}
