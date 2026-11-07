// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Storage.Graphs;

/// <summary>
/// Represents the operations one event caused in a graph.
/// </summary>
/// <param name="Owner">The <see cref="ProjectionId"/> of the projection that owns what the operations write.</param>
/// <param name="Operations">The <see cref="GraphOperation">operations</see>, in the order they must be applied.</param>
/// <remarks>
/// A changeset holds at most one write per element: all operations share one event sequence number, so a second write
/// to the same element would fall at or below the watermark the first one set.
/// </remarks>
public record GraphChangeset(ProjectionId Owner, IReadOnlyList<GraphOperation> Operations)
{
    /// <summary>
    /// Gets whether the changeset holds any operations.
    /// </summary>
    public bool HasChanges => Operations.Count > 0;
}
