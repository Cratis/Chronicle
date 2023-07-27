// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Engines.Keys;

namespace Aksio.Cratis.Kernel.Engines.Observation.Indexing;

/// <summary>
/// Defines a system for indexing keys for an observer.
/// </summary>
public interface IObserverKeyIndex
{
    /// <summary>
    /// Get the keys for a specific microservice and tenant.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the observer is for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the observer is for.</param>
    /// <returns>All the <see cref="IObserverKeys"/>.</returns>
    Task<IObserverKeys> GetKeysFor(MicroserviceId microserviceId, TenantId tenantId);

    /// <summary>
    /// Add a key to the index.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the observer is for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the observer is for.</param>
    /// <param name="key"><see cref="Key"/> to add.</param>
    /// <returns>Awaitable task.</returns>
    Task Add(MicroserviceId microserviceId, TenantId tenantId, Key key);

    /// <summary>
    /// Rebuild the index.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Rebuild();
}
