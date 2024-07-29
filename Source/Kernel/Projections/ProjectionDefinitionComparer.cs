// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Definitions;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represent an implementation of <see cref="IProjectionDefinitionComparer"/>.
/// </summary>
[Singleton]
public class ProjectionDefinitionComparer : IProjectionDefinitionComparer
{
    /// <inheritdoc/>
    public ProjectionDefinitionCompareResult Compare(ProjectionDefinition first, ProjectionDefinition second) => ProjectionDefinitionCompareResult.Different;
}
