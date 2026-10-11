// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>
/// Defines a system that can coordinate the effort around projections.
/// </summary>
public interface IProjectionPipeline
{
    /// <summary>
    /// Notifies about the beginning of a replay.
    /// </summary>
    /// <param name="context">The <see cref="ReplayContext"/> for the replay.</param>
    /// <returns>Awaitable task.</returns>
    Task BeginReplay(ReplayContext context);

    /// <summary>
    /// Notifies about the resuming of a replay.
    /// </summary>
    /// <param name="context">The <see cref="ReplayContext"/> for the replay.</param>
    /// <returns>Awaitable task.</returns>
    Task ResumeReplay(ReplayContext context);

    /// <summary>
    /// Notifies about the end of a replay.
    /// </summary>
    /// <param name="context">The <see cref="ReplayContext"/> for the replay.</param>
    /// <returns>The partitions whose writes failed during the final flush.</returns>
    Task<IEnumerable<FailedPartition>> EndReplay(ReplayContext context);

    /// <summary>
    /// Stop treating writes as part of a replay that has already been ended elsewhere, without promoting anything.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task LeaveReplay();

    /// <summary>
    /// Begin bulk operation mode.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task BeginBulk();

    /// <summary>
    /// End bulk operation mode.
    /// </summary>
    /// <returns>The partitions whose writes failed during the final flush.</returns>
    Task<IEnumerable<FailedPartition>> EndBulk();

    /// <summary>
    /// Handles the event and coordinates everything according to the pipeline.
    /// </summary>
    /// <param name="event"><see cref="AppendedEvent"/> to handle.</param>
    /// <returns>The <see cref="ProjectionEventContext"/> with the result of processing.</returns>
    Task<ProjectionEventContext> Handle(AppendedEvent @event);
}
