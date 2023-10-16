// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Keys;

/// <summary>
/// Defines a system for managing <see cref="IObserverKeyIndex">observer key indexes</see>.
/// </summary>
public interface IObserverKeyIndexes
{
    /// <summary>
    /// Get the key index for a specific observer, event sequence, microservice and tenant.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the index is for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the index is for.</param>
    /// <param name="observerId"><see cref="ObserverId"/> the index is for.</param>
    /// <param name="fromEventSequenceNumber">The <see cref="EventSequenceNumber"/> representing the first event instance you'd want to get keys.</param>
    /// <returns><see cref="IObserverKeyIndex"/> to work with.</returns>
    Task<IObserverKeyIndex> GetFor(MicroserviceId microserviceId, TenantId tenantId, ObserverId observerId, EventSequenceNumber fromEventSequenceNumber);
}
