// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Discovers and builds projection definitions from model-bound attributes.
/// </summary>
/// <param name="clientArtifactsProvider">The <see cref="IClientArtifactsProvider"/> to use for discovering client artifacts.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for providing event type information.</param>
public class ModelBoundProjections(
    IClientArtifactsProvider clientArtifactsProvider,
    INamingPolicy namingPolicy,
    IEventTypes eventTypes) : IModelBoundProjections
{
    /// <summary>
    /// Discovers all model-bound projections.
    /// </summary>
    /// <returns>A collection of <see cref="ProjectionDefinition"/>.</returns>
    public IEnumerable<ProjectionDefinition> Discover()
    {
        var builder = new ModelBoundProjectionBuilder(namingPolicy, eventTypes);
        return clientArtifactsProvider.ModelBoundProjections.Select(builder.Build);
    }
}
