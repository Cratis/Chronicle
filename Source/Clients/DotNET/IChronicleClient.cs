// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;

namespace Cratis.Chronicle;

/// <summary>
/// Defines the Cratis client API surface.
/// </summary>
public interface IChronicleClient
{
    /// <summary>
    /// Gets the <see cref="ChronicleOptions"/> for the client.
    /// </summary>
    ChronicleOptions Options { get; }

    /// <summary>
    /// Gets the <see cref="ICausationManager"/> for the client.
    /// </summary>
    ICausationManager CausationManager { get; }

    /// <summary>
    /// Get an event store by name and optional namespace.
    /// </summary>
    /// <param name="name">Name of the event store to get.</param>
    /// <param name="namespace">Optional namespace.</param>
    /// <param name="skipDiscovery">Whether to skip discovery.</param>
    /// <returns><see cref="IEventStore"/>.</returns>
    /// <remarks>
    /// If no namespace is specified, the default namespace will be used.
    /// </remarks>
    Task<IEventStore> GetEventStore(EventStoreName name, EventStoreNamespaceName? @namespace = default, bool skipDiscovery = false);

    /// <summary>
    /// List all the event stores.
    /// </summary>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>An asynchronous enumerable.</returns>
    Task<IEnumerable<EventStoreName>> GetEventStores(CancellationToken cancellationToken = default);
}
