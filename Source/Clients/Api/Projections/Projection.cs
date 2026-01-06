// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Api.Projections;

/// <summary>
/// Represents a projection.
/// </summary>
/// <param name="Identifier">Identifier of the projection.</param>
/// <param name="ReadModel">Name of the read model used.</param>
[ReadModel]
public record Projection(string Identifier, string ReadModel)
{
    /// <summary>
    /// Gets all projections.
    /// </summary>
    /// <param name="projections"><see cref="IProjections"/> for interacting with projections.</param>
    /// <param name="eventStore">The event store to get projections for.</param>
    /// <returns>All projections.</returns>
    public static async Task<IEnumerable<Projection>> AllProjections(IProjections projections, string eventStore)
    {
        var definitions = await projections.GetAllDefinitions(new GetAllDefinitionsRequest { EventStore = eventStore });
        return definitions.Select(d => new Projection(d.Identifier, d.ReadModel));
    }
}
