// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Converts failed partitions into the read model the failed-partition queries answer with.
/// </summary>
internal static class FailedPartitionDetailsConverters
{
    /// <summary>
    /// Converts failed partitions to their read model representation.
    /// </summary>
    /// <param name="failedPartitions">The failed partitions to convert.</param>
    /// <returns>The converted failed partitions.</returns>
    internal static IEnumerable<FailedPartitionDetails> ToReadModel(this IEnumerable<Concepts.Observation.FailedPartition> failedPartitions) =>
        [.. failedPartitions.Select(ToReadModel)];

    /// <summary>
    /// Converts a failed partition to its read model representation.
    /// </summary>
    /// <param name="failedPartition">The failed partition to convert.</param>
    /// <returns>The converted failed partition.</returns>
    internal static FailedPartitionDetails ToReadModel(this Concepts.Observation.FailedPartition failedPartition) =>
        new(
            failedPartition.Id,
            failedPartition.ObserverId.ToString(),
            failedPartition.Partition.ToString(),
            [.. failedPartition.Attempts.Select(ToReadModel)]);

    /// <summary>
    /// Converts a failed-partition attempt to its read model representation.
    /// </summary>
    /// <param name="attempt">The attempt to convert.</param>
    /// <returns>The converted attempt.</returns>
    internal static FailedPartitionAttemptDetails ToReadModel(this Concepts.Observation.FailedPartitionAttempt attempt) =>
        new(attempt.Occurred, attempt.SequenceNumber, attempt.Messages, attempt.StackTrace);
}
