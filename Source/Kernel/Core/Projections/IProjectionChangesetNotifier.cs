// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines the per-projection grain that fans projection changesets out to subscribed observers.
/// </summary>
/// <remarks>
/// The grain is keyed by the projection identifier. Replaces the previous Orleans MemoryStreams
/// pub-sub which had non-deterministic subscriber-propagation timing; direct grain-to-observer
/// dispatch gives end-to-end delivery the instant <see cref="Subscribe"/> returns.
/// </remarks>
public interface IProjectionChangesetNotifier : IGrainWithStringKey
{
    /// <summary>
    /// Subscribe an observer to receive changeset notifications.
    /// </summary>
    /// <param name="observer">The <see cref="IProjectionChangesetObserver"/> to subscribe.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Subscribe(IProjectionChangesetObserver observer);

    /// <summary>
    /// Unsubscribe an observer from changeset notifications.
    /// </summary>
    /// <param name="observer">The <see cref="IProjectionChangesetObserver"/> to unsubscribe.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Unsubscribe(IProjectionChangesetObserver observer);

    /// <summary>
    /// Notify all subscribed observers of a new changeset.
    /// </summary>
    /// <param name="namespaceName">The <see cref="EventStoreNamespaceName"/> the changeset belongs to.</param>
    /// <param name="readModelKey">The <see cref="ReadModelKey"/> identifying the read model instance.</param>
    /// <param name="readModel">The serialized read model as a <see cref="JsonObject"/>.</param>
    /// <param name="change">The <see cref="ReadModelChangeContext"/> describing the change and the event that caused it.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Notify(EventStoreNamespaceName namespaceName, ReadModelKey readModelKey, JsonObject readModel, ReadModelChangeContext change);
}
