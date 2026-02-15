// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Extension methods for converting between <see cref="Concepts.Observation.FailedPartition"/> and <see cref="FailedPartition"/>.
/// </summary>
internal static class FailedPartitionConverters
{
    /// <summary>
    /// Convert from <see cref="Concepts.Observation.FailedPartition"/> to <see cref="FailedPartition"/>.
    /// </summary>
    /// <param name="failedPartitions">Collection of <see cref="Concepts.Observation.FailedPartition"/> to convert from.</param>
    /// <returns>Converted collection of <see cref="FailedPartition"/>.</returns>
    public static IEnumerable<FailedPartition> ToContract(this IEnumerable<Concepts.Observation.FailedPartition> failedPartitions) =>
        failedPartitions.Select(_ => _.ToContract());

    /// <summary>
    /// Convert from <see cref="Concepts.Observation.FailedPartition"/> to <see cref="FailedPartition"/>.
    /// </summary>
    /// <param name="failedPartition"><see cref="Concepts.Observation.FailedPartition"/> to convert from.</param>
    /// <returns>Converted <see cref="FailedPartition"/>.</returns>
    public static FailedPartition ToContract(this Concepts.Observation.FailedPartition failedPartition)
    {
        return new FailedPartition
        {
            Id = failedPartition.Id,
            ObserverId = failedPartition.ObserverId.ToString(),
            Partition = failedPartition.Partition.ToString(),
            Attempts = failedPartition.Attempts.Select(_ => _.ToContract())
        };
    }

    /// <summary>
    /// Convert from <see cref="FailedPartitionAttempt"/> to <see cref="Concepts.Observation.FailedPartitionAttempt"/>.
    /// </summary>
    /// <param name="failedPartitionAttempt"><see cref="Concepts.Observation.FailedPartitionAttempt"/> to convert from.</param>
    /// <returns>Converted <see cref="FailedPartitionAttempt"/>.</returns>
    public static FailedPartitionAttempt ToContract(this Concepts.Observation.FailedPartitionAttempt failedPartitionAttempt)
    {
        return new FailedPartitionAttempt
        {
            Occurred = failedPartitionAttempt.Occurred!,
            SequenceNumber = failedPartitionAttempt.SequenceNumber,
            Messages = failedPartitionAttempt.Messages,
            StackTrace = failedPartitionAttempt.StackTrace
        };
    }
}
