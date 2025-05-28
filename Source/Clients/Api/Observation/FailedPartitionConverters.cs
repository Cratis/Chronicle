// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Observation;

/// <summary>
/// Converters between contracts and API models.
/// </summary>
internal static class FailedPartitionConverters
{
    /// <summary>
    /// Converts a <see cref="Contracts.Observation.FailedPartition"/> to a <see cref="FailedPartition"/>.
    /// </summary>
    /// <param name="failedPartition">The failed partition to convert.</param>
    /// <returns>The converted failed partition.</returns>
    public static FailedPartition ToApi(this Contracts.Observation.FailedPartition failedPartition) =>
        new(failedPartition.Id, failedPartition.ObserverId, failedPartition.Partition, failedPartition.Attempts.ToApi());

    /// <summary>
    /// Converts a collection of <see cref="Contracts.Observation.FailedPartition"/> to a collection of <see cref="FailedPartition"/>.
    /// </summary>
    /// <param name="failedPartitions">The failed partitions to convert.</param>
    /// <returns>The converted failed partitions.</returns>
    public static IEnumerable<FailedPartition> ToApi(this IEnumerable<Contracts.Observation.FailedPartition> failedPartitions) =>
        failedPartitions.Select(ToApi);
}
