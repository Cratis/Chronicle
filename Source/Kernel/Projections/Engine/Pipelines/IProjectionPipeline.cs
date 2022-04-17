// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store;

namespace Aksio.Cratis.Events.Projections.Pipelines;

/// <summary>
/// Defines a system that can coordinate the effort around projections.
/// </summary>
public interface IProjectionPipeline
{
    /// <summary>
    /// Gets the <see cref="IProjection"/> the pipeline is for.
    /// </summary>
    IProjection Projection { get; }

    /// <summary>
    /// Gets the <see cref="IProjectionSink">sink</see> to use for output.
    /// </summary>
    IProjectionSink Sink { get; }

    /// <summary>
    /// Handles the event and coordinates everything according to the pipeline.
    /// </summary>
    /// <param name="event"><see cref="AppendedEvent"/> to handle.</param>
    /// <returns>Awaitable task.</returns>
    Task Handle(AppendedEvent @event);
}
