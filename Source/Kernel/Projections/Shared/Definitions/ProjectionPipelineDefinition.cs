// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Definitions;

/// <summary>
/// Represents the definition of a projection pipeline.
/// </summary>
/// <param name="ProjectionId">Projection it is for.</param>
/// <param name="Sinks">Collection of <see cref="ProjectionSinkDefinition"/>.</param>
public record ProjectionPipelineDefinition(ProjectionId ProjectionId, IEnumerable<ProjectionSinkDefinition> Sinks);
