// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.ProjectionEngine.Pipelines;

/// <summary>
/// Defines a system that can coordinate the effort around projections.
/// </summary>
public interface IProjectionPipeline
{
    /// <summary>
    /// Notifies about the beginning of a replay.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task BeginReplay();

    /// <summary>
    /// Notifies about the end of a replay.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task EndReplay();

    /// <summary>
    /// Handles the event and coordinates everything according to the pipeline.
    /// </summary>
    /// <param name="event"><see cref="AppendedEvent"/> to handle.</param>
    /// <returns>Awaitable task.</returns>
    Task Handle(AppendedEvent @event);
}
