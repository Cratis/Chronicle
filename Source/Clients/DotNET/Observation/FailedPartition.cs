// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents a failed partition.
/// </summary>
/// <param name="Id">Unique identifier of the failed partition registration.</param>
/// <param name="ObserverId">The identifier of the observer (Reactor, Reducer).</param>
/// <param name="Partition">Partition that has failed.</param>
/// <param name="Attempts">Collection of <see cref="FailedPartitionAttempt"/>.</param>
public record FailedPartition(FailedPartitionId Id, ObserverId ObserverId, Partition Partition, IEnumerable<FailedPartitionAttempt> Attempts)
{
    /// <summary>
    /// Gets whether the failure has been resolved.
    /// </summary>
    public bool IsResolved { get; init; }

    /// <summary>
    /// Gets whether the partition is quarantined from automatic retries.
    /// Null means the connected server did not provide this additive state.
    /// </summary>
    public bool? IsQuarantined { get; init; }
}
