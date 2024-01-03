// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Persistence;

/// <summary>
/// Defines the shared storage for an event store.
/// </summary>
public interface IEventStoreStorage
{
    /// <summary>
    /// Get a specific <see cref="IEventStoreInstanceStorage"/> for a <see cref="TenantId"/>.
    /// </summary>
    /// <param name="tenantId">The <see cref="TenantId"/> to get for.</param>
    /// <returns>The <see cref="IEventStoreInstanceStorage"/> instance.</returns>
    IEventStoreInstanceStorage GetInstance(TenantId tenantId);
}
