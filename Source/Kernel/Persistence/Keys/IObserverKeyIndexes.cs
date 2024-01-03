// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Persistence.Keys;

/// <summary>
/// Defines a system for managing <see cref="IObserverKeyIndex">observer key indexes</see>.
/// </summary>
public interface IObserverKeyIndexes
{
    /// <summary>
    /// Get the key index for a specific observer, event sequence, microservice and tenant.
    /// </summary>
    /// <param name="observerId"><see cref="ObserverId"/> the index is for.</param>
    /// <param name="observerKey"><see cref="ObserverKey"/> for the observer.</param>
    /// <returns><see cref="IObserverKeyIndex"/> to work with.</returns>
    Task<IObserverKeyIndex> GetFor(ObserverId observerId, ObserverKey observerKey);
}
