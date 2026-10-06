// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// The exception that is thrown when known bulk write failures must accompany an interrupted operation.
/// </summary>
/// <param name="failedPartitions">Partitions whose writes are known to have failed.</param>
/// <param name="innerException">The failure that interrupted the write, if any.</param>
public class BulkWriteFailed(IEnumerable<FailedPartition> failedPartitions, Exception? innerException = null)
    : Exception("The bulk write failed; its failed partitions require retry.", innerException)
{
    /// <summary>
    /// Gets the partitions whose writes are known to have failed.
    /// </summary>
    public IReadOnlyList<FailedPartition> FailedPartitions { get; } = failedPartitions.ToArray();
}
