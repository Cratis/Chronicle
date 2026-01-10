// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents a projection with its DSL representation.
/// </summary>
/// <param name="Identifier">The projection identifier.</param>
/// <param name="ReadModel">The read model the projection projects to.</param>
/// <param name="Dsl">The projection DSL.</param>
public record ProjectionWithDsl(ProjectionId Identifier, ReadModelName ReadModel, ProjectionDsl Dsl);
