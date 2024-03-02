// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Projections.Definitions;

/// <summary>
/// Represents a combination of a projection and a pipeline for it.
/// </summary>
/// <param name="Projection">The <see cref="ProjectionDefinition"/>.</param>
/// <param name="Pipeline">The <see cref="ProjectionPipelineDefinition"/>.</param>
public record ProjectionAndPipeline(ProjectionDefinition Projection, ProjectionPipelineDefinition Pipeline);
