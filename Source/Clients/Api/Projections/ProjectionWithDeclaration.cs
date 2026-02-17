// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Api.Projections;

/// <summary>
/// Represents a projection along with its projection declaration language representation.
/// </summary>
/// <param name="Identifier">Identifier of the projection.</param>
/// <param name="ContainerName">The container name of the read model the projection projects to (collection, table, etc.).</param>
/// <param name="Declaration">Declaration in the form of projection declaration language representation of the projection.</param>
[ReadModel]
public record ProjectionWithDeclaration(string Identifier, string ContainerName, string Declaration)
{
    /// <summary>
    /// Gets all projections with their declaration representation.
    /// </summary>
    /// <param name="projections"><see cref="IProjections"/> for interacting with projections.</param>
    /// <param name="eventStore">The event store to get projections for.</param>
    /// <returns>All projections with their declaration representation.</returns>
    internal static async Task<IEnumerable<ProjectionWithDeclaration>> AllProjectionsWithDeclarations(IProjections projections, string eventStore)
    {
        var declarations = await projections.GetAllDeclarations(new GetAllDeclarationsRequest { EventStore = eventStore });
        return declarations.Select(d => new ProjectionWithDeclaration(d.Identifier, d.ContainerName, d.Declaration));
    }
}
