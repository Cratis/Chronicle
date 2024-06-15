// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Defines the Cratis client API surface.
/// </summary>
public interface ICratisClient
{
    /// <summary>
    /// Get an event store by name and optional namespace.
    /// </summary>
    /// <param name="name">Name of the event store to get.</param>
    /// <param name="namespace">Optional namespace.</param>
    /// <returns><see cref="IEventStore"/>.</returns>
    /// <remarks>
    /// If no namespace is specified, the default namespace will be used.
    /// </remarks>
    IEventStore GetEventStore(EventStoreName name, EventStoreNamespaceName? @namespace = default);

    /// <summary>
    /// List all the event stores.
    /// </summary>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>An asynchronous enumerable.</returns>
    IAsyncEnumerable<EventStoreName> ListEventStores(CancellationToken cancellationToken = default);
}
