// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;

namespace Cratis.Chronicle.Storage.MongoDB.Projections;

/// <summary>
/// Represents a projection.
/// </summary>
/// <param name="Id">The unique identifier of the projection.</param>
/// <param name="Owner">The owner of the projection.</param>
/// <param name="ReadModel">The read model associated with the projection.</param>
/// <param name="Definitions">The definitions per generation of the projection.</param>
public record Projection(
    ProjectionId Id,
    ProjectionOwner Owner,
    Concepts.ReadModels.ReadModelType ReadModel,
    IDictionary<string, ProjectionDefinition> Definitions);
