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
public record FailedPartition(FailedPartitionId Id, ObserverId ObserverId, Partition Partition, IEnumerable<FailedPartitionAttempt> Attempts);
