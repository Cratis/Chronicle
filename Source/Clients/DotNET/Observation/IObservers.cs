// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a system for working with the observers of an event store.
/// </summary>
public interface IObservers
{
    /// <summary>
    /// Get every observer registered in the event store's current namespace.
    /// </summary>
    /// <returns>A collection of <see cref="ObserverInformation"/>.</returns>
    Task<IEnumerable<ObserverInformation>> GetAll();

    /// <summary>
    /// Remove an observer and everything keyed to it.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> of the observer to remove.</param>
    /// <returns>An <see cref="ObserverRemovalResult"/> describing what happened.</returns>
    /// <remarks>
    /// For the observer whose declaring code is gone - a read model and its projection that were deleted, a reactor
    /// that was removed. The observer it registered stays behind, settles into
    /// <see cref="ObserverRunningState.Disconnected"/> and keeps its records in the event store forever.
    /// <para>
    /// Removal covers the whole event store, because an observer's definition is a store-level record. It refuses
    /// while the observer is running or has a subscribed client in any namespace, so what it can remove is only ever
    /// an observer no client is reporting - stop the declaring application first if you mean to remove a live one.
    /// Read model data and sink containers are left untouched.
    /// </para>
    /// </remarks>
    Task<ObserverRemovalResult> Remove(ObserverId observerId);
}
