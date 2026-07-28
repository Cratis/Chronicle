// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents configuration for read models.
/// </summary>
public class ReadModels
{
    /// <summary>
    /// Gets the number of replayed read model versions to retain.
    /// Older replayed collections are removed when a new replay starts.
    /// </summary>
    public int ReplayedVersionsToKeep { get; init; } = 1;

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
    /// hierarchy) is deliberately written out of order and is never guarded. Turning this off restores the previous
    /// behavior exactly.
    /// </remarks>
    public bool GuardSinkWritesOnWatermark { get; init; } = true;
}
