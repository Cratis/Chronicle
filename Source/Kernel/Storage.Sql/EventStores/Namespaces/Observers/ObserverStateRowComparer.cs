// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers;

/// <summary>
/// Compares observer state rows structurally, including the running state timestamp, so SQL live queries
/// emit exactly when a persisted column changed.
/// </summary>
/// <remarks>
/// Serialized columns are compared as strings, so no deserialization or mapping happens per poll.
/// </remarks>
public sealed class ObserverStateRowComparer : IEqualityComparer<ObserverState>, IEqualityComparer
{
    /// <summary>
    /// Gets the shared comparer instance.
    /// </summary>
    public static readonly ObserverStateRowComparer Instance = new();

    /// <inheritdoc/>
    public bool Equals(ObserverState? x, ObserverState? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return SameOwnership(x, y) &&
            SameSerializedColumns(x, y) &&
            SameAlert(x, y) &&
            SameProgress(x, y) &&
            SameFlags(x, y) &&
            SameCollections(x, y);
    }

    /// <inheritdoc/>
    public int GetHashCode(ObserverState obj) => obj.Id.GetHashCode();

    /// <inheritdoc/>
    bool IEqualityComparer.Equals(object? x, object? y) => Equals(x as ObserverState, y as ObserverState);

    /// <inheritdoc/>
    int IEqualityComparer.GetHashCode(object obj) => GetHashCode((ObserverState)obj);

    static bool SameOwnership(ObserverState x, ObserverState y) =>
        x.Id == y.Id &&
        x.Owner == y.Owner &&
        x.OwnershipRevision == y.OwnershipRevision &&
        x.RunningStateChangedAt == y.RunningStateChangedAt;

    static bool SameSerializedColumns(ObserverState x, ObserverState y) =>
        x.PartitionClaims == y.PartitionClaims &&
        x.ObserverWideClaim == y.ObserverWideClaim &&
        x.TypeSetControl == y.TypeSetControl &&
        x.Initialization == y.Initialization &&
        x.DeliveryFences == y.DeliveryFences &&
        x.WebhookProbe == y.WebhookProbe;

    static bool SameAlert(ObserverState x, ObserverState y) =>
        x.AlertLifecycleId == y.AlertLifecycleId &&
        x.AlertRevision == y.AlertRevision &&
        x.AlertDisposition == y.AlertDisposition &&
        x.QuarantineEpisodeId == y.QuarantineEpisodeId;

    static bool SameProgress(ObserverState x, ObserverState y) =>
        x.LastHandledEventSequenceNumber == y.LastHandledEventSequenceNumber &&
        x.NextEventSequenceNumber == y.NextEventSequenceNumber &&
        x.TailEventSequenceNumber == y.TailEventSequenceNumber &&
        x.HandledEventCount == y.HandledEventCount;

    static bool SameFlags(ObserverState x, ObserverState y) =>
        x.RunningState == y.RunningState &&
        x.IsReplaying == y.IsReplaying &&
        x.SubscribesToAllEvents == y.SubscribesToAllEvents &&
        x.FailedPartitionCount == y.FailedPartitionCount;

    static bool SameCollections(ObserverState x, ObserverState y) =>
        SameSet(x.ReplayingPartitions, y.ReplayingPartitions) &&
        SameSet(x.CatchingUpPartitions, y.CatchingUpPartitions) &&
        SameCounts(x.HandledEventCountPerEventType, y.HandledEventCountPerEventType) &&
        SameFailedPartitions(x.FailedPartitions, y.FailedPartitions);

    static bool SameSet(ISet<string>? left, ISet<string>? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Count == right.Count && left.SetEquals(right);
    }

    static bool SameCounts(IDictionary<string, ulong>? left, IDictionary<string, ulong>? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null || left.Count != right.Count) return false;

        foreach (var (key, value) in left)
        {
            if (!right.TryGetValue(key, out var other) || other != value) return false;
        }

        return true;
    }

    static bool SameFailedPartitions(IEnumerable<FailedPartition>? left, IEnumerable<FailedPartition>? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;

        using var leftEnumerator = left.GetEnumerator();
        using var rightEnumerator = right.GetEnumerator();
        while (leftEnumerator.MoveNext())
        {
            if (!rightEnumerator.MoveNext() || !SameFailedPartition(leftEnumerator.Current, rightEnumerator.Current)) return false;
        }

        return !rightEnumerator.MoveNext();
    }

    static bool SameFailedPartition(FailedPartition x, FailedPartition y) =>
        SameFailureIdentity(x, y) &&
        SameFailureStatus(x, y) &&
        SameFailureRecovery(x, y) &&
        SameAttempts(x.Attempts, y.Attempts);

    static bool SameFailureIdentity(FailedPartition x, FailedPartition y) =>
        x.Id == y.Id && x.ObserverId == y.ObserverId && x.Partition == y.Partition;

    static bool SameFailureStatus(FailedPartition x, FailedPartition y) =>
        x.IsResolved == y.IsResolved &&
        x.IsQuarantined == y.IsQuarantined &&
        x.WebhookDisposition == y.WebhookDisposition &&
        x.WebhookRecoveryId == y.WebhookRecoveryId;

    static bool SameFailureRecovery(FailedPartition x, FailedPartition y) =>
        x.AttemptsBeforeBudgetReset == y.AttemptsBeforeBudgetReset &&
        Equals(x.RecoveryCursor, y.RecoveryCursor) &&
        Equals(x.RecoveryOwner, y.RecoveryOwner) &&
        Equals(x.Hold, y.Hold);

    static bool SameAttempts(IEnumerable<FailedPartitionAttempt> left, IEnumerable<FailedPartitionAttempt> right)
    {
        using var leftEnumerator = left.GetEnumerator();
        using var rightEnumerator = right.GetEnumerator();
        while (leftEnumerator.MoveNext())
        {
            if (!rightEnumerator.MoveNext() || !SameAttempt(leftEnumerator.Current, rightEnumerator.Current)) return false;
        }

        return !rightEnumerator.MoveNext();
    }

    static bool SameAttempt(FailedPartitionAttempt x, FailedPartitionAttempt y) =>
        x.Occurred == y.Occurred &&
        x.SequenceNumber == y.SequenceNumber &&
        x.Kind == y.Kind &&
        string.Equals(x.StackTrace, y.StackTrace, StringComparison.Ordinal) &&
        x.Messages.SequenceEqual(y.Messages, StringComparer.Ordinal) &&
        SameFence(x.DeliveryFence, y.DeliveryFence);

    static bool SameFence(DeliveryFenceContext? x, DeliveryFenceContext? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null || x.ReleaseBy != y.ReleaseBy) return false;
        if (ReferenceEquals(x.Targets, y.Targets)) return true;
        return x.Targets is not null && y.Targets is not null && x.Targets.SequenceEqual(y.Targets);
    }
}
