// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// The exception that is thrown when a bulk write cannot determine the outcome of its remaining operations.
/// </summary>
/// <param name="failedPartitions">Partitions requiring retry, including failures known before the write was interrupted.</param>
/// <param name="innerException">The failure that interrupted the write.</param>
public class BulkWriteFailed(IEnumerable<FailedPartition> failedPartitions, Exception innerException)
    : Exception("The bulk write was interrupted; its failed partitions require retry.", innerException)
{
    /// <summary>
    /// Gets the partitions whose writes failed or have an unknown outcome.
    /// </summary>
    public IReadOnlyList<FailedPartition> FailedPartitions { get; } = failedPartitions.ToArray();
}
