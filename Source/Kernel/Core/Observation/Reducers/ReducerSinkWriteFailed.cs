// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers;

/// <summary>
/// The exception that is thrown when the sink reports failed partitions for a reducer's write.
/// </summary>
/// <remarks>
/// A bulk flush can report partitions other than the one being reduced. Every one of them is carried, so the caller
/// can record each as failed instead of losing all but the first.
/// </remarks>
/// <param name="failedPartitions">The <see cref="FailedPartition"/> instances the sink reported.</param>
public class ReducerSinkWriteFailed(IReadOnlyCollection<FailedPartition> failedPartitions)
    : Exception($"Sink write failed for {failedPartitions.Count} partition(s): {string.Join(", ", failedPartitions.Select(MessageFor))}")
{
    /// <summary>
    /// Gets the <see cref="FailedPartition"/> instances the sink reported.
    /// </summary>
    public IReadOnlyCollection<FailedPartition> FailedPartitions { get; } = failedPartitions;

    /// <summary>
    /// Describes one failed partition.
    /// </summary>
    /// <param name="failedPartition">The <see cref="FailedPartition"/> to describe.</param>
    /// <returns>The description.</returns>
    public static string MessageFor(FailedPartition failedPartition) =>
        $"partition {failedPartition.EventSourceId} at sequence number {failedPartition.EventSequenceNumber}" +
        (string.IsNullOrEmpty(failedPartition.Reason) ? string.Empty : $": {failedPartition.Reason}");
}
