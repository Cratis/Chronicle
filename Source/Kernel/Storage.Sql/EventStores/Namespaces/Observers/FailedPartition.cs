// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers;

/// <summary>
/// Represents a failed partition for SQL storage.
/// </summary>
/// <param name="Id">Unique identifier of the failed partition.</param>
/// <param name="Partition">The partition that is failed.</param>
/// <param name="ObserverId">The observer identifier for which this is a failed partition.</param>
/// <param name="Attempts">Collection of failed partition attempts.</param>
/// <param name="IsResolved">Whether the failure is resolved.</param>
public record FailedPartition(
    string Id,
    string Partition,
    string ObserverId,
    IEnumerable<FailedPartitionAttempt> Attempts,
    bool IsResolved);
