// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Defines a projection.
/// </summary>
public interface IProjection : IGrainWithStringKey
{
    /// <summary>
    /// Set the projection definition and subscribe as an observer.
    /// </summary>
    /// <param name="definition"><see cref="ProjectionDefinition"/> to refresh with.</param>
    /// <returns>Awaitable task.</returns>
    Task SetDefinition(ProjectionDefinition definition);

    /// <summary>
    /// Get the projection definition.
    /// </summary>
    /// <returns>The current <see cref="ProjectionDefinition"/>.</returns>
    Task<ProjectionDefinition> GetDefinition();

    /// <summary>
    /// Subscribe to changes in projection or pipeline definition changes.
    /// </summary>
    /// <param name="subscriber"><see cref="INotifyProjectionDefinitionsChanged"/> to subscribe.</param>
    /// <returns>Awaitable task.</returns>
    Task SubscribeDefinitionsChanged(INotifyProjectionDefinitionsChanged subscriber);

    /// <summary>
    /// Unsubscribe to changes in projection or pipeline definition changes.
    /// </summary>
    /// <param name="subscriber"><see cref="INotifyProjectionDefinitionsChanged"/> to subscribe.</param>
    /// <returns>Awaitable task.</returns>
    Task UnsubscribeDefinitionsChanged(INotifyProjectionDefinitionsChanged subscriber);

    /// <summary>
    /// Deactivate all projection grains (observer subscribers and immediate projections) to force reload.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task DeactivateGrains();
}
