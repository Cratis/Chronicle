// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Selects the failed partitions that belong to the progress of one event sequence.
/// </summary>
/// <remarks>
/// Failures tagged with another event sequence belong to the progress parked for that sequence. Untagged failures
/// predate per-sequence progress and belong to whichever sequence owns the observer.
/// </remarks>
internal static class FailedPartitionsForEventSequence
{
    /// <summary>
    /// Gets the failed partitions visible to the given event sequence.
    /// </summary>
    /// <param name="partitions">All stored failed partitions of the observer.</param>
    /// <param name="eventSequenceId">The event sequence the observer is addressed on.</param>
    /// <returns>The failed partitions that belong to the event sequence.</returns>
    public static FailedPartition[] VisibleTo(this IEnumerable<FailedPartition> partitions, EventSequenceId eventSequenceId) =>
        partitions.Where(partition => partition.EventSequenceId is null || partition.EventSequenceId == eventSequenceId).ToArray();
}
