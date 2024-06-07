// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.DependencyInjection;
using Cratis.Integration;
using Cratis.Kernel.Contracts.Projections;
using Cratis.Rules;

namespace Cratis.Projections;

/// <summary>
/// Represents an implementation of <see cref="IClientProjections"/>.
/// </summary>
[Singleton]
public class ClientProjections : IClientProjections
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClientProjections"/> class.
    /// </summary>
    /// <param name="projections"><see cref="IProjections"/> for projections defined by <see cref="IProjectionBuilderFor{T}"/>.</param>
    /// <param name="immediateProjections">All the <see cref="IImmediateProjections"/>.</param>
    /// <param name="adapters"><see cref="IAdapters"/> for getting adapters projection definitions.</param>
    /// <param name="rulesProjections"><see cref="IRulesProjections"/> for getting projection definitions related to rules.</param>
    public ClientProjections(
        IProjections projections,
        IImmediateProjections immediateProjections,
        IAdapters adapters,
        IRulesProjections rulesProjections)
    {
        var projectionDefinitions = new List<ProjectionDefinition>();
        projectionDefinitions.AddRange(projections.Definitions);
        projectionDefinitions.AddRange(immediateProjections.Definitions);
        projectionDefinitions.AddRange(adapters.Definitions);
        projectionDefinitions.AddRange(rulesProjections.Definitions);
        Definitions = projectionDefinitions.ToImmutableList();
    }

    /// <inheritdoc/>
    public IImmutableList<ProjectionDefinition> Definitions { get; }
}
