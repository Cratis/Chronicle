// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Represents everything that is registered for a graph: the read model that describes it and the projection that fills it.
/// </summary>
/// <param name="GraphName">The name of the graph.</param>
/// <param name="ReadModel">The <see cref="ReadModelDefinition"/> whose schema describes the graph.</param>
/// <param name="Projection">The <see cref="ProjectionDefinition"/> with the graph mappings.</param>
internal record GraphRegistration(string GraphName, ReadModelDefinition ReadModel, ProjectionDefinition Projection);
