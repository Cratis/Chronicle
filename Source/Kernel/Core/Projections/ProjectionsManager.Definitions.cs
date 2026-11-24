// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections;

public partial class ProjectionsManager
{
    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectionDefinition>> GetProducersFor(ReadModelIdentifier readModel, ProjectionId? namedProjection)
    {
        var stored = await storage.GetEventStore(_eventStoreName).Projections.GetProducersFor(readModel, namedProjection);
        return State.Projections.Concat(_registeringDefinitions.Values).Concat(stored)
            .Where(definition => definition.ReadModel == readModel || definition.Identifier == namedProjection)
            .ToArray();
    }
}
