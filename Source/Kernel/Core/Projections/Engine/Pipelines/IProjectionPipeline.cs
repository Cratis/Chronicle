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
    /// Join the sink's open bulk window, or begin one when none is open.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ResumeBulk();

    /// <summary>
    /// Drain the sink's buffered writes without closing its bulk window.
    /// </summary>
    /// <returns>The window identity and any buffered write failures.</returns>
    Task<BulkFlushResult> FlushBulk();

    /// <summary>
    /// End bulk operation mode.
    /// </summary>
    /// <returns>The partitions whose writes failed during the final flush.</returns>
    Task<IEnumerable<FailedPartition>> EndBulk();

    /// <summary>
    /// Ends the bulk window within the shutdown deadline.
    /// </summary>
    /// <param name="cancellationToken">The shared deadline token.</param>
    /// <returns>The failed or unknown partitions.</returns>
    Task<IEnumerable<FailedPartition>> EndBulk(CancellationToken cancellationToken) => EndBulk();

    /// <summary>
    /// Prepares the pipeline for a page of events that is about to be handled, one partition run at a time.
    /// </summary>
    /// <param name="events">The events of the page, in the order they will be handled.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Called once per page before any of its events are handled. It is an optimization only: the default does nothing,
    /// and a failure is logged and ignored, so it must never be required for correct handling.
    /// </remarks>
    Task PreparePage(IReadOnlyList<AppendedEvent> events) => Task.CompletedTask;

    /// <summary>
    /// Discards optional read preparation owned by the completed page, without discarding pending writes.
    /// </summary>
    void ClearPreparedPage()
    {
    }

    /// <summary>
    /// Handles the event and coordinates everything according to the pipeline.
    /// </summary>
    /// <param name="event"><see cref="AppendedEvent"/> to handle.</param>
    /// <returns>The <see cref="ProjectionEventContext"/> with the result of processing.</returns>
    Task<ProjectionEventContext> Handle(AppendedEvent @event);
}
