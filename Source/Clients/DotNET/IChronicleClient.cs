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
    /// Gets the <see cref="global::Cratis.Chronicle.Federation.IFederationEnrollments"/> for the kernel the client is connected to: creating, redeeming and revoking hosted federation enrollments and reading their status.
    /// </summary>
    /// <exception cref="NotSupportedException">The client implementation does not support federation enrollment.</exception>
    global::Cratis.Chronicle.Federation.IFederationEnrollments Federation => throw new NotSupportedException("Federation enrollment is not supported by this client.");

    /// <summary>
    /// Gets the <see cref="global::Cratis.Chronicle.Federation.IFederationPolicies"/> for the kernel the client is connected to: setting, suspending, resuming and revoking the policy of hosted federation subjects and reading their policies and link status.
    /// </summary>
    /// <exception cref="NotSupportedException">The client implementation does not support federation policies.</exception>
    global::Cratis.Chronicle.Federation.IFederationPolicies FederationPolicies => throw new NotSupportedException("Federation policies are not supported by this client.");

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
    /// Gets an event store with a registration mode captured for this handle, without changing the client's default.
    /// </summary>
    /// <param name="name">Name of the event store.</param>
    /// <param name="namespace">Namespace for the event store. Omit with <see langword="null"/> to resolve it as for the default overload.</param>
    /// <param name="registrationMode">Registration mode for the lifetime of this handle, including reconnection and manual registration.</param>
    /// <returns>The event store, cached by name, resolved namespace, and registration mode.</returns>
    /// <remarks>
    /// <see cref="RegistrationMode.EventTypesOnly"/> ensures the store and registers the local event catalog, but no other artifacts.
    /// <see cref="RegistrationMode.AtDeployment"/> performs no automatic kernel registration or provisioning; the owner must provide these.
    /// Both modes retain local discovery and serialization. Neither enables <see cref="ChronicleOptions.AutoDiscoverAndRegister"/> when disabled,
    /// nor restricts explicit artifact API calls made by the caller. Different modes produce separate handles sharing the same connection.
    /// </remarks>
    Task<IEventStore> GetEventStore(EventStoreName name, EventStoreNamespaceName? @namespace, RegistrationMode registrationMode);

    /// <summary>
    /// List all the event stores.
    /// </summary>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>An asynchronous enumerable.</returns>
    Task<IEnumerable<EventStoreName>> GetEventStores(CancellationToken cancellationToken = default);

    /// <summary>
    /// Evict every cached <see cref="IEventStore"/>, unsubscribing each one's <c language="csharp">RegisterAll</c>
    /// handler from the shared connection lifecycle.
    /// </summary>
    /// <remarks>
    /// Used to release per-event-store subscriptions when the calling environment needs to
    /// fully reset client state without disposing the client itself — for example, between
    /// integration test classes that share an <see cref="IChronicleClient"/> instance. Without
    /// this, every <c language="csharp">IEventStore</c> ever created stays subscribed to <c language="csharp">OnConnected</c> and
    /// re-runs <c language="csharp">RegisterAll</c> on subsequent reconnects, causing the kernel to receive a
    /// fanout of concurrent <c language="csharp">Ensure</c> calls for the same event-store name.
    /// </remarks>
    void EvictEventStores();
}
