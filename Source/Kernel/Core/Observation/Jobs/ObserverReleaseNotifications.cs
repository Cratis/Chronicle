// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Tells an observer that a job's work is over, carrying the ownership epoch the job was started under.
/// </summary>
/// <remarks>
/// The observer ignores a report whose epoch is no longer its claim's, so a stale job cannot release the claim of the
/// newer job that superseded it. A request without an epoch was issued by a silo that grants no claims; it reports
/// through the methods that predate epochs, which every silo in a mixed-version cluster serves.
/// </remarks>
public static class ObserverReleaseNotifications
{
    /// <summary>
    /// Reports that a partition catch-up was caught up.
    /// </summary>
    /// <param name="observer">The <see cref="IObserver"/> to report to.</param>
    /// <param name="request">The <see cref="IObserverJobRequest"/> the job runs.</param>
    /// <param name="partition">The partition that has caught up.</param>
    /// <param name="lastHandled">The last handled event.</param>
    /// <param name="lastScanned">The last event read, or unavailable when the catch-up did not complete.</param>
    /// <returns>Awaitable task.</returns>
    public static Task PartitionCaughtUp(this IObserver observer, IObserverJobRequest request, Key partition, EventSequenceNumber lastHandled, EventSequenceNumber lastScanned) =>
        request.OwnershipEpoch is null
            ? observer.PartitionCaughtUp(partition, lastHandled, lastScanned)
            : observer.PartitionCaughtUp(partition, lastHandled, lastScanned, request.OwnershipEpoch);

    /// <summary>
    /// Reports that a partition replay finished.
    /// </summary>
    /// <param name="observer">The <see cref="IObserver"/> to report to.</param>
    /// <param name="request">The <see cref="IObserverJobRequest"/> the job runs.</param>
    /// <param name="partition">The partition being replayed.</param>
    /// <param name="lastHandled">The last handled event.</param>
    /// <param name="lastScanned">The last event read.</param>
    /// <param name="replayedEventTypes">The event types included in the replay.</param>
    /// <returns>Awaitable task.</returns>
    public static Task PartitionReplayed(this IObserver observer, IObserverJobRequest request, Key partition, EventSequenceNumber lastHandled, EventSequenceNumber lastScanned, EventType[] replayedEventTypes) =>
        request.OwnershipEpoch is null
            ? observer.PartitionReplayed(partition, lastHandled, lastScanned, replayedEventTypes)
            : observer.PartitionReplayed(partition, lastHandled, lastScanned, replayedEventTypes, request.OwnershipEpoch);

    /// <summary>
    /// Releases a refused partition replay.
    /// </summary>
    /// <param name="observer">The <see cref="IObserver"/> to report to.</param>
    /// <param name="request">The <see cref="IObserverJobRequest"/> the job runs.</param>
    /// <param name="partition">The partition whose replay was refused.</param>
    /// <param name="heldBackFrom">The first held-back position.</param>
    /// <returns>Awaitable task.</returns>
    public static Task ReleaseRefusedPartitionReplay(this IObserver observer, IObserverJobRequest request, Key partition, EventSequenceNumber heldBackFrom) =>
        request.OwnershipEpoch is null
            ? observer.ReleaseRefusedPartitionReplay(partition, heldBackFrom)
            : observer.ReleaseRefusedPartitionReplay(partition, heldBackFrom, request.OwnershipEpoch);

    /// <summary>
    /// Reports that a replay finished without proving complete coverage.
    /// </summary>
    /// <param name="observer">The <see cref="IObserver"/> to report to.</param>
    /// <param name="request">The <see cref="IObserverJobRequest"/> the job runs.</param>
    /// <param name="lastHandled">The last handled event.</param>
    /// <returns>Awaitable task.</returns>
    public static Task Replayed(this IObserver observer, IObserverJobRequest request, EventSequenceNumber lastHandled) =>
        request.OwnershipEpoch is null
            ? observer.Replayed(lastHandled)
            : observer.Replayed(lastHandled, request.OwnershipEpoch);

    /// <summary>
    /// Reports that a replay completed successfully.
    /// </summary>
    /// <param name="observer">The <see cref="IObserver"/> to report to.</param>
    /// <param name="request">The <see cref="IObserverJobRequest"/> the job runs.</param>
    /// <param name="lastHandled">The last handled event.</param>
    /// <param name="replayedPartitions">Partitions and their successfully handled sequence numbers.</param>
    /// <param name="replayedEventTypes">The event types read during the replay.</param>
    /// <param name="replayStartedAt">The time before replay steps were prepared.</param>
    /// <returns>Awaitable task.</returns>
    public static Task ReplayedSuccessfullySince(this IObserver observer, IObserverJobRequest request, EventSequenceNumber lastHandled, IReadOnlyDictionary<Key, EventSequenceNumber> replayedPartitions, EventType[] replayedEventTypes, DateTimeOffset replayStartedAt) =>
        request.OwnershipEpoch is null
            ? observer.ReplayedSuccessfullySince(lastHandled, replayedPartitions, replayedEventTypes, replayStartedAt)
            : observer.ReplayedSuccessfullySince(lastHandled, replayedPartitions, replayedEventTypes, replayStartedAt, request.OwnershipEpoch);

    /// <summary>
    /// Releases a refused full replay.
    /// </summary>
    /// <param name="observer">The <see cref="IObserver"/> to report to.</param>
    /// <param name="request">The <see cref="IObserverJobRequest"/> the job runs.</param>
    /// <returns>Awaitable task.</returns>
    public static Task ReleaseRefusedReplay(this IObserver observer, IObserverJobRequest request) =>
        request.OwnershipEpoch is null
            ? observer.ReleaseRefusedReplay()
            : observer.ReleaseRefusedReplay(request.OwnershipEpoch);
}
