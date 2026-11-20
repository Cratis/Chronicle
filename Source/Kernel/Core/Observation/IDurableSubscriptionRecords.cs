// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines the read side of the durable subscriptions observers record in their state (decision 0034).
/// </summary>
/// <remarks>
/// Everything that must know whether an observer is subscribed without waking it reads it here: the keeper of a client's
/// stream, the projections manager, the unseen-observer removal and the sweeps of departed clients and silos. The records
/// of a namespace are read once and shared for a short while, so a thousand observers cost one read.
/// </remarks>
public interface IDurableSubscriptionRecords
{
    /// <summary>
    /// Gets whether a durable subscription can be relied on as the subscription of an observer right now.
    /// </summary>
    /// <remarks>
    /// True while dormant release applies: the setting is on and every silo of the cluster supports it. Otherwise a
    /// subscription lives in the observer's activation only, and a record says nothing about it.
    /// </remarks>
    bool CanBeRelied { get; }

    /// <summary>
    /// Gets whether the open stream of a client confirms its subscriptions from the durable subscription.
    /// </summary>
    bool ConfirmsClientSubscriptions { get; }

    /// <summary>
    /// Gets the durable subscription of one observer.
    /// </summary>
    /// <param name="observer">The <see cref="ObserverKey"/> of the observer.</param>
    /// <returns>The durable subscription, or null when the observer has none recorded.</returns>
    Task<ObserverDurableSubscription?> Get(ObserverKey observer);

    /// <summary>
    /// Gets the durable subscription of every observer of an event sequence in a namespace that has recorded one.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/>.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/>.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> that owns the observers.</param>
    /// <param name="fresh">Whether to read storage rather than reuse a recent read.</param>
    /// <returns>The durable subscription by observer.</returns>
    Task<IReadOnlyDictionary<ObserverId, ObserverDurableSubscription>> GetAll(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, bool fresh = false);

    /// <summary>
    /// Gets whether the durable subscription of an observer names a connection among its targets.
    /// </summary>
    /// <param name="observer">The <see cref="ObserverKey"/> of the observer.</param>
    /// <param name="connectionId">The <see cref="ConnectionId"/> of the client.</param>
    /// <returns>True when the record names the connection.</returns>
    Task<bool> Names(ObserverKey observer, ConnectionId connectionId);
}
