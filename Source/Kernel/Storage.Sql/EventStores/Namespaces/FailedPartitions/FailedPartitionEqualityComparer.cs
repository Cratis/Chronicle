// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions;

/// <summary>
/// Compares failed-partition snapshots structurally so SQL live queries emit only real changes.
/// </summary>
public sealed class FailedPartitionEqualityComparer :
    IEqualityComparer<Concepts.Observation.FailedPartition>,
    IEqualityComparer
{
    /// <summary>
    /// Gets the shared comparer instance.
    /// </summary>
    public static readonly FailedPartitionEqualityComparer Instance = new();

    /// <inheritdoc/>
    public bool Equals(Concepts.Observation.FailedPartition? x, Concepts.Observation.FailedPartition? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return x.Id == y.Id &&
            x.ObserverId == y.ObserverId &&
            x.Partition == y.Partition &&
            x.IsResolved == y.IsResolved &&
            x.IsQuarantined == y.IsQuarantined &&
            AttemptsEqual(x.Attempts, y.Attempts);
    }

    /// <inheritdoc/>
    public int GetHashCode(Concepts.Observation.FailedPartition obj) => obj.Id.GetHashCode();

    /// <inheritdoc/>
    bool IEqualityComparer.Equals(object? x, object? y) =>
        Equals(x as Concepts.Observation.FailedPartition, y as Concepts.Observation.FailedPartition);

    /// <inheritdoc/>
    int IEqualityComparer.GetHashCode(object obj) => GetHashCode((Concepts.Observation.FailedPartition)obj);

    static bool AttemptsEqual(
        IEnumerable<Concepts.Observation.FailedPartitionAttempt> left,
        IEnumerable<Concepts.Observation.FailedPartitionAttempt> right) =>
        left.Zip(right, (leftAttempt, rightAttempt) =>
            leftAttempt.Occurred == rightAttempt.Occurred &&
            leftAttempt.SequenceNumber == rightAttempt.SequenceNumber &&
            leftAttempt.Messages.SequenceEqual(rightAttempt.Messages, StringComparer.Ordinal) &&
            string.Equals(leftAttempt.StackTrace, rightAttempt.StackTrace, StringComparison.Ordinal))
            .All(_ => _) &&
        left.Count() == right.Count();
}
