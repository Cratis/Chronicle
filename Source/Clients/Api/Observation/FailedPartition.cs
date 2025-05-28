// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Observation;

/// <summary>
/// Represents a failed partition.
/// </summary>
/// <param name="Id">The unique identifier of the failed partition.</param>
/// <param name="ObserverId">The observer id.</param>
/// <param name="Partition">The partition that is failed.</param>
/// <param name="Attempts">The attempts for the failed partition.</param>
public record FailedPartition(Guid Id, string ObserverId, string Partition, IEnumerable<FailedPartitionAttempt> Attempts);
