// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Api.Projections;

/// <summary>
/// Represents a projection along with its DSL representation.
/// </summary>
/// <param name="Identifier">Identifier of the projection.</param>
/// <param name="ReadModel">The read model the projection projects to.</param>
/// <param name="Dsl">DSL representation of the projection.</param>
[ReadModel]
public record ProjectionWithDsl(string Identifier, string ReadModel, string Dsl)
{
    /// <summary>
    /// Gets all projections with their DSL representation.
    /// </summary>
    /// <param name="projections"><see cref="IProjections"/> for interacting with projections.</param>
    /// <param name="eventStore">The event store to get projections for.</param>
    /// <returns>All projections with their DSL representation.</returns>
    public static async Task<IEnumerable<ProjectionWithDsl>> AllProjectionsWithDsl(IProjections projections, string eventStore)
    {
        var dsls = await projections.GetAllDsls(new GetAllDslsRequest { EventStore = eventStore });
        return dsls.Select(d => new ProjectionWithDsl(d.Identifier, d.ReadModel, d.Dsl));
    }
}
