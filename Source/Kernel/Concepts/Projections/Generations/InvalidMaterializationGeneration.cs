// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations;

/// <summary>
/// The exception that is thrown when a generation identity is derived for a generation that cannot exist.
/// </summary>
/// <param name="projectionId">The <see cref="ProjectionId"/> the generation was derived for.</param>
/// <param name="generation">The invalid <see cref="MaterializationGeneration"/>.</param>
public class InvalidMaterializationGeneration(ProjectionId projectionId, MaterializationGeneration generation)
    : Exception($"Generation {generation.Value} is not a valid materialization generation for projection '{projectionId.Value}'. Generations start at {MaterializationGeneration.First.Value}.");
