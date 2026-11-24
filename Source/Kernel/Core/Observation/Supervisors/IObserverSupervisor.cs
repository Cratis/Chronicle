// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.ObserverRouting;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Supervisors;

/// <summary>
/// Defines the supervisor of one observer definition: one per event store and observer, shared by every namespace.
/// </summary>
/// <remarks>
/// The supervisor owns the definition axis of an observer, the part every namespace shares. It is never on the event path:
/// events are matched against the silo-local routing table and delivered to the observer of each namespace directly.
/// </remarks>
public interface IObserverSupervisor : IGrainWithStringKey
{
    /// <summary>
    /// Publish the stored revision of the observer's definition to the routing table of every active silo.
    /// </summary>
    /// <returns>The <see cref="ObserverRevisionAcknowledgements"/> of the silos.</returns>
    Task<ObserverRevisionAcknowledgements> Publish();

    /// <summary>
    /// Find the namespaces in which the observer has events waiting for it, without activating any observer.
    /// </summary>
    /// <returns>The <see cref="ObserverKey"/> of the observer in every namespace it is behind in, or could not be compared in.</returns>
    Task<IEnumerable<ObserverKey>> FindBehind();

    /// <summary>
    /// Records which connection serves the observer in a namespace: a hint held in memory and never persisted.
    /// </summary>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> of the instance.</param>
    /// <param name="binding">The <see cref="ObserverDurableSubscription"/> that describes the subscription and its targets.</param>
    /// <returns>Awaitable task.</returns>
    Task Bind(EventStoreNamespaceName @namespace, ObserverDurableSubscription binding);

    /// <summary>
    /// Forgets which connection serves the observer in a namespace.
    /// </summary>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> of the instance.</param>
    /// <returns>Awaitable task.</returns>
    Task Unbind(EventStoreNamespaceName @namespace);

    /// <summary>
    /// Gets which connection serves the observer in a namespace, as last told.
    /// </summary>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> of the instance.</param>
    /// <returns>The <see cref="ObserverDurableSubscription"/>, or null when none is known.</returns>
    /// <remarks>
    /// A hint: an instance that activates without a subscription validates every target against the connected clients before
    /// it uses one, and a wrong or missing one is covered by its cursor and catch-up.
    /// </remarks>
    Task<ObserverDurableSubscription?> GetBinding(EventStoreNamespaceName @namespace);
}
