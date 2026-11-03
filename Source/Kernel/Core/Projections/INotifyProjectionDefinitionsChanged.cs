// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines an internal grain observer that lets you get notified when definitions change on a projection.
/// </summary>
/// <remarks>
/// On <see cref="ImmediateProjection"/>, this notification may interleave with an active read.
/// Acknowledgment means the definition was received, not that the active read adopted it; the latest
/// pending definition is applied after that read completes or fails. The active read keeps its existing
/// read model metadata, while subsequent reads resolve the current definition from the projection grain.
/// </remarks>
public interface INotifyProjectionDefinitionsChanged : IGrainObserver
{
    /// <summary>
    /// Called when a projection definition is changed.
    /// </summary>
    /// <param name="definition">The new <see cref="ProjectionDefinition"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task OnProjectionDefinitionsChanged(ProjectionDefinition definition);
}
