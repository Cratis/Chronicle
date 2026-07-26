// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines the per-projection grain that fans projection changesets out to the subscriber grains of
/// watching clients.
/// </summary>
/// <remarks>
/// The grain is keyed by the projection identifier and has a single activation cluster-wide, so it is the
/// topology-correct rendezvous between the shared projection observer (which may run on any silo) and the
/// per-connection watch subscribers (each pinned to the silo terminating its client connection). Fan-out
/// is to <see cref="IReadModelChangesetSubscriber"/> grain references — reliably routed by Orleans — which
/// replaces the previous <c>CreateObjectReference</c> grain-observer callback whose one-way dispatch was
/// silently dropped on slower backends.
/// </remarks>
public interface IProjectionChangesetNotifier : IGrainWithStringKey
{
    /// <summary>
    /// Subscribe a watch subscriber grain to receive changeset notifications.
    /// </summary>
    /// <param name="subscriber">The <see cref="IReadModelChangesetSubscriber"/> to subscribe.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Subscribe(IReadModelChangesetSubscriber subscriber);

    /// <summary>
    /// Unsubscribe a watch subscriber grain from changeset notifications.
    /// </summary>
    /// <param name="subscriber">The <see cref="IReadModelChangesetSubscriber"/> to unsubscribe.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Unsubscribe(IReadModelChangesetSubscriber subscriber);

    /// <summary>
    /// Notify all subscribed watch subscribers of a new changeset.
    /// </summary>
    /// <param name="namespaceName">The <see cref="EventStoreNamespaceName"/> the changeset belongs to.</param>
    /// <param name="readModelKey">The <see cref="ReadModelKey"/> identifying the read model instance.</param>
    /// <param name="readModel">The serialized read model as a <see cref="JsonObject"/>.</param>
    /// <param name="change">The <see cref="ReadModelChangeContext"/> describing the change and the event that caused it.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Notify(EventStoreNamespaceName namespaceName, ReadModelKey readModelKey, JsonObject readModel, ReadModelChangeContext change);
}
