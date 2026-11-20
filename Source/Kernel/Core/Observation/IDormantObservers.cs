// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
}
