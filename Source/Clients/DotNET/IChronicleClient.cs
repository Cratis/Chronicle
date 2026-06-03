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
    /// <returns><see cref="IEventStore"/>.</returns>
    /// <remarks>
    /// If no namespace is specified, the default namespace will be used.
    /// </remarks>
    Task<IEventStore> GetEventStore(EventStoreName name, EventStoreNamespaceName? @namespace = default);

    /// <summary>
    /// List all the event stores.
    /// </summary>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>An asynchronous enumerable.</returns>
    Task<IEnumerable<EventStoreName>> GetEventStores(CancellationToken cancellationToken = default);

    /// <summary>
    /// Evict every cached <see cref="IEventStore"/>, unsubscribing each one's <c>RegisterAll</c>
    /// handler from the shared connection lifecycle.
    /// </summary>
    /// <remarks>
    /// Used to release per-event-store subscriptions when the calling environment needs to
    /// fully reset client state without disposing the client itself — for example, between
    /// integration test classes that share an <see cref="IChronicleClient"/> instance. Without
    /// this, every <c>IEventStore</c> ever created stays subscribed to <c>OnConnected</c> and
    /// re-runs <c>RegisterAll</c> on subsequent reconnects, causing the kernel to receive a
    /// fanout of concurrent <c>Ensure</c> calls for the same event-store name.
    /// </remarks>
    void EvictEventStores();
}
