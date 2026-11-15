// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

/// <summary>
/// The result of compiling a graph declaration: the projection definition and the read model that holds the graph.
/// </summary>
/// <param name="Projection">The <see cref="ProjectionDefinition"/> that builds the graph.</param>
/// <param name="ReadModel">The <see cref="ReadModelDefinition"/> describing the graph as a read model.</param>
public record CompiledGraph(ProjectionDefinition Projection, ReadModelDefinition ReadModel);
