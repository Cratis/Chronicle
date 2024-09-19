// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represent an implementation of <see cref="IProjectionDefinitionComparer"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
[Singleton]
public class ProjectionDefinitionComparer(IStorage storage, IObjectComparer objectComparer) : IProjectionDefinitionComparer
{
    /// <inheritdoc/>
    public async Task<ProjectionDefinitionCompareResult> Compare(
        ProjectionKey projectionKey,
        ProjectionDefinition first,
        ProjectionDefinition second)
    {
        if (!await storage.GetEventStore(projectionKey.EventStore).Projections.Has(projectionKey.ProjectionId))
        {
            return ProjectionDefinitionCompareResult.New;
        }

        if (!objectComparer.Compare(first, second, out _))
        {
            return ProjectionDefinitionCompareResult.Different;
        }

        return ProjectionDefinitionCompareResult.Same;
    }
}
