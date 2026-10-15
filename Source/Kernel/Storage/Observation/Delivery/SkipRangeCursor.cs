// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Sequencing;

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Represents a strictly-after skip cursor ordered by ordinal key hash, then start position.
/// </summary>
/// <param name="KeyHash">The key hash.</param>
/// <param name="From">The inclusive range start.</param>
public sealed record SkipRangeCursor(SequencingKeyHash KeyHash, EventSequenceNumber From)
{
    /// <summary>
    /// Creates the next page cursor from the last item.
    /// </summary>
    /// <param name="range">The last skip range.</param>
    /// <returns>The strictly-after cursor.</returns>
    public static SkipRangeCursor For(SkipRange range) => new(range.Key.Hash, range.From);
}
