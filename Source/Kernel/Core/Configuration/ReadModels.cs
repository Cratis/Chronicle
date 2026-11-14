// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents configuration for read models.
/// </summary>
public class ReadModels
{
    /// <summary>
    /// Gets the maximum number of items handled as one unit: events per subscriber call when replaying a projection, and documents per fenced sink write.
    /// </summary>
    public int MaxBatchSize { get; init; } = 500;

    /// <summary>
    /// Gets the number of replayed read model versions to retain.
    /// Older replayed collections are removed when a new replay starts.
    /// </summary>
    public int ReplayedVersionsToKeep { get; init; } = 1;

    /// <summary>
    /// Gets the snapshot retention count. This is an alias of <see cref="ReplayedVersionsToKeep"/>; either configuration key sets the same value.
    /// </summary>
    public int SnapshotsToKeep
    {
        get => ReplayedVersionsToKeep;
        init => ReplayedVersionsToKeep = value;
    }

    /// <summary>
    /// Gets how long retired generations keep following for rollback. Takes effect only when <see cref="Features.ProjectionGenerations"/> is enabled.
    /// </summary>
    public TimeSpan RollbackWindow { get; init; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Gets the maximum number of retired generations that keep following. Takes effect only when <see cref="Features.ProjectionGenerations"/> is enabled.
    /// </summary>
    public int MaxFollowingRetiredGenerations { get; init; } = 1;

    /// <summary>
    /// Gets a value indicating whether a read model write is made conditional on the incoming event advancing the
    /// read model's last handled event sequence number.
    /// </summary>
    /// <remarks>
    /// A crash on the catch-up path resumes a job step from its last durable checkpoint and re-delivers every event
    /// handled since, bypassing the observer's cursor filter. Accumulating projections and reducer folds are
    /// read-modify-write and are corrupted by that redelivery. With this enabled — the default — the write is
    /// applied only when it moves the watermark forward, making the redelivery a no-op. It is requested only for
    /// read models whose documents are keyed by event source id, where the per-document event stream is monotonic;
    /// a projection whose key collapses several event sources onto one document (a join, a constant key or a parent
    /// hierarchy) is deliberately written out of order and is never guarded, with one exception: a projection keyed by the
    /// event source id that has root joins keyed by the event source id is written once per source event, tracked by applied
    /// event markers in the document, which happens only when <see cref="RecordAppliedEventMarkers"/> is turned on.
    /// Turning this off restores the previous behavior exactly.
    /// </remarks>
    public bool GuardSinkWritesOnWatermark { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether a projection marked for applied event markers writes its data and its markers in one
    /// guarded update, making a redelivered, stale or late event a no-op.
    /// </summary>
    /// <remarks>
    /// Applies to the MongoDB sink only, and to projections keyed by the event source id that have root joins keyed by the
    /// event source id (see the applied event marker classification). It requires MongoDB 5.0 or later,
    /// because the write is an aggregation pipeline update. It takes effect only when <see cref="GuardSinkWritesOnWatermark"/>
    /// is also turned on. Off by default. Markers left in documents from an earlier period are stale, so before turning this on
    /// again after a period off, drain the writers and either unset the __applied field on the affected collections or replay the projection.
    /// </remarks>
    public bool RecordAppliedEventMarkers { get; init; }
}
