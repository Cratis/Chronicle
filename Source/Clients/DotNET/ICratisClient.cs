// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis;

/// <summary>
/// Defines the Cratis client API surface.
/// </summary>
public interface ICratisClient
{
    /// <summary>
    /// Get an event store by name and optional tenant.
    /// </summary>
    /// <param name="name">Name of the event store to get.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <returns><see cref="IEventStore"/>.</returns>
    /// <remarks>
    /// If no tenant is specified, the default tenant will be used.
    /// In a single tenanted environment, the default tenant is what you'd be using.
    /// </remarks>
    IEventStore GetEventStore(EventStoreName name, TenantId? tenantId = default);

    /// <summary>
    /// List all the event stores.
    /// </summary>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>An asynchronous enumerable.</returns>
    IAsyncEnumerable<EventStoreName> ListEventStores(CancellationToken cancellationToken = default);
}
