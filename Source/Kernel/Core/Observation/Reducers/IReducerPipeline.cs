// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers;

/// <summary>
/// Defines a system that can coordinate the effort around reducers.
/// </summary>
public interface IReducerPipeline
{
    /// <summary>
    /// Gets the <see cref="ReadModelDefinition"/> the pipeline is for.
    /// </summary>
    ReadModelDefinition ReadModel { get; }

    /// <summary>
    /// Gets the <see cref="ISink">sink</see> to use for output.
    /// </summary>
    ISink Sink { get; }

    /// <summary>
    /// Notifies about the beginning of a replay.
    /// </summary>
    /// <param name="context">The <see cref="ReplayContext"/> for the replay.</param>
    /// <returns>Awaitable task.</returns>
    Task BeginReplay(ReplayContext context);

    /// <summary>
    /// Notifies about the end of a replay.
    /// </summary>
    /// <param name="context">The <see cref="ReplayContext"/> for the replay.</param>
    /// <returns>The partitions whose writes failed during the final flush.</returns>
    Task<IEnumerable<FailedPartition>> EndReplay(ReplayContext context);

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
    /// Gets the size the state of a partition had when it was last reduced, as far as the pipeline remembers it.
    /// </summary>
    /// <param name="partition">The <see cref="Key"/> of the partition.</param>
    /// <returns>The estimated size in bytes, or zero when it is not known.</returns>
    /// <remarks>
    /// A page holds the state of every partition in it, so the state counts toward the page's byte bound. It is not known
    /// before it has been read, so the size it had last time stands in for it.
    /// </remarks>
    long StateSizeOf(Key partition);

    /// <summary>
    /// Handles the event and coordinates everything according to the pipeline.
    /// </summary>
    /// <param name="context">The <see cref="ReducerContext"/> being reduced.</param>
    /// <param name="reducer"><see cref="ReducerDelegate"/> delegate.</param>
    /// <param name="createNotification">Whether to build the notification of the change, a whole new copy of the state; false when nothing is watching.</param>
    /// <returns>The committed change to notify watchers of, or null if there was no change.</returns>
    Task<ReadModelChangeNotification?> Reduce(ReducerContext context, ReducerDelegate reducer, bool createNotification = true);

    /// <summary>
    /// Reduces a page of partitions in one delivery, settling each partition on its own.
    /// </summary>
    /// <param name="contexts">The <see cref="ReducerContext"/> of every partition in the page, in the order to write them.</param>
    /// <param name="reducer"><see cref="ReducerPageDelegate"/> that reduces every partition of the page in one call.</param>
    /// <param name="createNotification">Whether to build the notification of each change; false when nothing is watching.</param>
    /// <returns>The <see cref="ReducerPageOutcome"/> with the result, the change and any failed write of each partition.</returns>
    /// <remarks>
    /// The states of every partition are read in one sink read where the sink can, and the successful states are written in
    /// one write where the sink can. A page never opens, flushes or closes the sink's shared bulk window, so every write is
    /// durable or reported as failed when the page returns; a window someone else holds open, such as a replay's, is joined.
    /// A partition the client failed or deferred writes nothing, and a failed write fails only its own partition. Each
    /// partition is reduced exactly as <see cref="Reduce"/> reduces it on its own.
    /// </remarks>
    Task<ReducerPageOutcome> ReducePage(IReadOnlyList<ReducerContext> contexts, ReducerPageDelegate reducer, bool createNotification = true);
}
