// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a per-watch subscriber grain that receives projection changesets from the
/// <see cref="IProjectionChangesetNotifier"/> and forwards them to the watching client's gRPC stream.
/// </summary>
/// <remarks>
/// The grain is keyed by an <c>ObserverSubscriberKey</c> whose silo address pins it (via
/// <c>[ConnectedObserverPlacement]</c>) to the silo that terminates the client's watch connection, so the
/// final hop through the in-process <see cref="IProjectionChangesetMediator"/> never leaves that silo.
/// The notifier reaches it as an ordinary — and therefore reliably routed — grain reference rather than a
/// <c>CreateObjectReference</c> object reference.
/// </remarks>
public interface IReadModelChangesetSubscriber : IGrainWithStringKey
{
    /// <summary>
    /// Called by the notifier when a projection produces a changeset for the watched read model.
    /// </summary>
    /// <param name="namespaceName">The <see cref="EventStoreNamespaceName"/> the changeset belongs to.</param>
    /// <param name="readModelKey">The <see cref="ReadModelKey"/> identifying the read model instance.</param>
    /// <param name="readModel">The serialized read model as a <see cref="JsonObject"/>.</param>
    /// <param name="change">The <see cref="ReadModelChangeContext"/> describing the change.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// Marked <see cref="OneWayAttribute"/>: read-model change notifications are best-effort, and the
    /// notifier must not block its grain turn on the downstream push to the client stream.
    /// </remarks>
    [OneWay]
    Task OnChangeset(EventStoreNamespaceName namespaceName, ReadModelKey readModelKey, JsonObject readModel, ReadModelChangeContext change);
}
