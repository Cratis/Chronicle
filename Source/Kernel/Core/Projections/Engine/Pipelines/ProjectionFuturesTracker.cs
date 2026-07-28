// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>
/// Tracks, for a single projection pipeline, whether futures may be awaiting resolution so the
/// per-event futures grain call can be skipped when there is genuinely nothing pending.
/// </summary>
/// <remarks>
/// A single instance is shared between the <see cref="Steps.StoreFutures"/> and
/// <see cref="Steps.ResolveFutures"/> steps of one pipeline. All access happens under the
/// pipeline's handle lock, which serializes every event, so no additional synchronization is
/// required.
/// </remarks>
public sealed class ProjectionFuturesTracker
{
    /// <summary>
    /// Gets or sets a value indicating whether futures may be pending resolution for the pipeline.
    /// </summary>
    /// <remarks>
    /// Starts <see langword="true"/> so a freshly created pipeline probes the durable futures state
    /// exactly once — futures stored by a previous pipeline instance (for example before a grain
    /// deactivation or a replay-driven pipeline eviction) survive in durable storage and must still
    /// be resolved. It is set to <see langword="false"/> only after resolution observes an empty
    /// futures store, so it can never report "none pending" while a future is actually stored.
    /// </remarks>
    public bool HasPending { get; set; } = true;
}
