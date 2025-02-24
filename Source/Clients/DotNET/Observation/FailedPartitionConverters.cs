// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Converter methods for <see cref="FailedPartition"/>.
/// </summary>
internal static class FailedPartitionConverters
{
    /// <summary>
    /// Convert from a <see cref="Contracts.Observation.FailedPartition"/> to a <see cref="FailedPartition"/>.
    /// </summary>
    /// <param name="failedPartition">The <see cref="Contracts.Observation.FailedPartition"/> to convert from.</param>
    /// <returns>Converted <see cref="FailedPartition"/>.</returns>
    internal static FailedPartition ToClient(this Contracts.Observation.FailedPartition failedPartition)
    {
        return new FailedPartition(
            failedPartition.Id,
            failedPartition.ObserverId,
            failedPartition.Partition,
            failedPartition.Attempts.Select(_ => _.ToClient()));
    }

    /// <summary>
    /// Convert from a collection of <see cref="Contracts.Observation.FailedPartition"/> to a collection of <see cref="FailedPartition"/>.
    /// </summary>
    /// <param name="failedPartitions">Collection of <see cref="Contracts.Observation.FailedPartition"/>.</param>
    /// <returns>A collection of <see cref="FailedPartition"/>.</returns>
    internal static IEnumerable<FailedPartition> ToClient(this IEnumerable<Contracts.Observation.FailedPartition> failedPartitions) =>
        failedPartitions.Select(_ => _.ToClient()).ToArray();

    /// <summary>
    /// Convert from a <see cref="Contracts.Observation.FailedPartitionAttempt"/> to a <see cref="FailedPartitionAttempt"/>.
    /// </summary>
    /// <param name="failedPartitionAttempt">The <see cref="Contracts.Observation.FailedPartitionAttempt"/> to convert from.</param>
    /// <returns>Converted <see cref="FailedPartitionAttempt"/>.</returns>
    internal static FailedPartitionAttempt ToClient(this Contracts.Observation.FailedPartitionAttempt failedPartitionAttempt)
    {
        return new FailedPartitionAttempt(
            failedPartitionAttempt.Occurred,
            failedPartitionAttempt.SequenceNumber,
            failedPartitionAttempt.Messages,
            failedPartitionAttempt.StackTrace);
    }
}
