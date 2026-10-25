// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Generations;

/// <summary>
/// The typed inputs restored from an immutable execution snapshot.
/// </summary>
/// <param name="Projection">The captured projection definition.</param>
/// <param name="ReadModel">The captured read model definition.</param>
/// <param name="EventSchemas">The captured source event schemas.</param>
/// <param name="SemanticSinkRevision">The captured credential-free sink revision.</param>
public sealed record MaterializationExecutionInputs(
    ProjectionDefinition Projection,
    ReadModelDefinition ReadModel,
    IReadOnlyCollection<EventTypeSchema> EventSchemas,
    MaterializationRevision SemanticSinkRevision);
