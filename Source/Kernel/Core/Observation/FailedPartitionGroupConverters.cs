// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Groups failed partitions into the read model the failed-partition group query answers with.
/// </summary>
internal static class FailedPartitionGroupConverters
{
    /// <summary>
    /// The most example partitions a group carries.
    /// </summary>
    internal const int MaximumExamples = 10;

    /// <summary>
    /// Groups the current, unresolved failed partitions by observer and the kind of their latest attempt.
    /// </summary>
    /// <param name="partitions">The failed partitions to group.</param>
    /// <returns>The groups, largest first and then most recently failed first.</returns>
    internal static IEnumerable<FailedPartitionGroup> ToGroups(this IEnumerable<Concepts.Observation.FailedPartition> partitions) =>
        [.. partitions
            .Where(partition => !partition.IsResolved)
            .GroupBy(partition => (partition.ObserverId, partition.LastAttempt.Kind))
            .Select(group => ToGroup(group.Key.ObserverId, group.Key.Kind, [.. group]))
            .OrderByDescending(group => group.PartitionCount)
            .ThenByDescending(group => group.LastObserved)];

    static FailedPartitionGroup ToGroup(ObserverId observerId, FailureKind kind, IReadOnlyList<Concepts.Observation.FailedPartition> partitions)
    {
        var withAttempts = partitions.Where(partition => partition.Attempts.Any()).ToArray();

        return new(
            $"{observerId}/{kind}",
            observerId.ToString(),
            kind,
            partitions.Count,
            partitions.Count(partition => partition.IsQuarantined),
            withAttempts.Length == 0 ? null : withAttempts.Min(partition => partition.Attempts.First().Occurred),
            withAttempts.Length == 0 ? null : withAttempts.Max(partition => partition.LastAttempt.Occurred),
            [.. partitions
                .OrderByDescending(partition => partition.Attempts.Any() ? partition.LastAttempt.Occurred : DateTimeOffset.MinValue)
                .Take(MaximumExamples)
                .Select(partition => partition.Partition.ToString())]);
    }
}
