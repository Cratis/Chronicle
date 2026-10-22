// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Projections.Generations;

/// <summary>
/// The exception that is thrown when a projection is asked to build a generation that belongs to a different projection.
/// </summary>
/// <param name="projectionId">The <see cref="ProjectionId"/> of the projection asked to build.</param>
/// <param name="generation">The <see cref="MaterializationGenerationIdentity"/> it was asked to build.</param>
public class GenerationBelongsToAnotherProjection(ProjectionId projectionId, MaterializationGenerationIdentity generation)
    : Exception($"Generation {generation.Generation.Value} belongs to projection '{generation.ProjectionId.Value}', not to '{projectionId.Value}'.");
