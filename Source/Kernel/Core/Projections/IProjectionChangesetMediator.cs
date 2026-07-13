// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a per-silo in-process mediator that forwards projection changesets to the gRPC watch
/// streams owned by that silo.
/// </summary>
/// <remarks>
/// This mirrors <c>IReducerMediator</c>/<c>IReactorMediator</c>: the gRPC <c>Watch</c> service registers
/// the connection's stream here, and the per-watch subscriber grain — placed on this silo so the hop is
/// strictly in-process — invokes <see cref="OnChangeset"/> to push a changeset onto that stream. This
/// replaces the previous Orleans <c>CreateObjectReference</c> grain-observer callback, whose one-way
/// dispatch from the notifier grain to a service-created object reference was silently dropped on slower
/// backends.
/// </remarks>
public interface IProjectionChangesetMediator
{
    /// <summary>
    /// Registers the forwarder for a watch subscription.
    /// </summary>
    /// <param name="subscriptionId">The unique identifier of the watch subscription.</param>
    /// <param name="forwarder">The <see cref="ChangesetForwarder"/> that pushes onto the client stream.</param>
    void Subscribe(Guid subscriptionId, ChangesetForwarder forwarder);

    /// <summary>
    /// Removes the forwarder for a watch subscription.
    /// </summary>
    /// <param name="subscriptionId">The unique identifier of the watch subscription.</param>
    void Unsubscribe(Guid subscriptionId);

    /// <summary>
    /// Forwards a changeset to the registered watch subscription, if it is still connected.
    /// </summary>
    /// <param name="subscriptionId">The unique identifier of the watch subscription.</param>
    /// <param name="namespaceName">The <see cref="EventStoreNamespaceName"/> the changeset belongs to.</param>
    /// <param name="readModelKey">The <see cref="ReadModelKey"/> identifying the read model instance.</param>
    /// <param name="readModel">The serialized read model as a <see cref="JsonObject"/>.</param>
    /// <param name="change">The <see cref="ReadModelChangeContext"/> describing the change.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task OnChangeset(Guid subscriptionId, EventStoreNamespaceName namespaceName, ReadModelKey readModelKey, JsonObject readModel, ReadModelChangeContext change);
}
