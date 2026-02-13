// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents a projection with its projection declaration language representation.
/// </summary>
/// <param name="Identifier">The projection identifier.</param>
/// <param name="ContainerName">The container name of the read model the projection projects to.</param>
/// <param name="Declaration">The projection definition as projection declaration language.</param>
public record ProjectionWithDeclaration(ProjectionId Identifier, ReadModelContainerName ContainerName, ProjectionDeclaration Declaration);
