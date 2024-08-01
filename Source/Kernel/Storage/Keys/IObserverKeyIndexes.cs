// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Keys;

/// <summary>
/// Defines a system for managing <see cref="IObserverKeyIndex">observer key indexes</see>.
/// </summary>
public interface IObserverKeyIndexes
{
    /// <summary>
    /// Get the key index for a specific observer, event sequence, event store and namespace.
    /// </summary>
    /// <param name="observerKey"><see cref="ObserverKey"/> for the observer.</param>
    /// <returns><see cref="IObserverKeyIndex"/> to work with.</returns>
    Task<IObserverKeyIndex> GetFor(ObserverKey observerKey);
}
