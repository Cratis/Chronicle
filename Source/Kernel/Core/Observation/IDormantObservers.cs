// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines how to tell that an observer is dormant, and what it is subscribed to, without activating it (decision 0034).
/// </summary>
/// <remarks>
/// Listing and inspecting observers must not wake the ones that were released: that would turn a look at a thousand
/// observers into a thousand activations. A dormant observer is subscribed, so these surfaces report it as subscribed from
/// its durable subscription instead.
/// </remarks>
public interface IDormantObservers
{
    /// <summary>
    /// Finds the durable subscription of an observer if, and only if, the observer is dormant right now.
    /// </summary>
    /// <param name="observer">The <see cref="ObserverKey"/> of the observer.</param>
    /// <returns>The durable subscription of the dormant observer; null when the observer is resident, not subscribed, or dormancy does not apply.</returns>
    /// <remarks>
    /// The appended events queue holding the observer's subscription knows whether it is dormant, so a resident observer is
    /// never mistaken for a dormant one on the strength of a record that may be a moment old.
    /// </remarks>
    Task<ObserverDurableSubscription?> Find(ObserverKey observer);

    /// <summary>
    /// Gets whether an observer is dormant and its client, served from this silo, is the one it recorded.
    /// </summary>
    /// <param name="observer">The <see cref="ObserverKey"/> of the observer.</param>
    /// <param name="connectionId">The <see cref="ConnectionId"/> of the client whose stream asks.</param>
    /// <returns>True when the subscription is intact without the observer being asked; false whenever it has to be asked.</returns>
    /// <remarks>
    /// A record alone does not say the subscription is intact: an observer that is resident when its silo is lost has a record
    /// and no live subscription. Only the queue that holds the observer as dormant says the record is the subscription, and
    /// only a target on this silo says the stream asking is the one the record names. Everything else, including the setting
    /// that decides whether the keeper may rely on records, answers false so the observer is asked as it always was.
    /// </remarks>
    Task<bool> HoldsClient(ObserverKey observer, ConnectionId connectionId);
}
