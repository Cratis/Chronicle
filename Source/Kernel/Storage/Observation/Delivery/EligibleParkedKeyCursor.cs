// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Sequencing;

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Represents a strictly-after cursor ordered by retry due, ready ticket, then ordinal key hash.
/// </summary>
/// <param name="RetryDue">The retry clock in UTC ticks.</param>
/// <param name="ReadyTicket">The ready ticket.</param>
/// <param name="KeyHash">The final ordering tiebreaker.</param>
public sealed record EligibleParkedKeyCursor(long RetryDue, DeliveryTicket ReadyTicket, SequencingKeyHash KeyHash)
{
    /// <summary>
    /// Creates the next page cursor from the last item.
    /// </summary>
    /// <param name="key">The last parked key.</param>
    /// <returns>The strictly-after cursor.</returns>
    public static EligibleParkedKeyCursor For(ParkedKey key) => new(key.RetryDue, key.ReadyTicket, key.Key.Hash);
}
