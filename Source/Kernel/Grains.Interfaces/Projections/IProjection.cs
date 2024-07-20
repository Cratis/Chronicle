// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Definitions;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Defines a projection.
/// </summary>
public interface IProjection : IGrainWithStringKey
{
    /// <summary>
    /// Refresh the projection definition.
    /// </summary>
    /// <param name="definition"><see cref="ProjectionDefinition"/> to refresh with.</param>
    /// <returns>Awaitable task.</returns>
    Task SetDefinition(ProjectionDefinition definition);

    /// <summary>
    /// Ensure the projection exists and is started.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Ensure();

    /// <summary>
    /// Subscribe to changes in projection or pipeline definition changes.
    /// </summary>
    /// <param name="subscriber"><see cref="INotifyProjectionDefinitionsChanged"/> to subscribe.</param>
    /// <returns>Awaitable task.</returns>
    Task SubscribeDefinitionsChanged(INotifyProjectionDefinitionsChanged subscriber);
}
