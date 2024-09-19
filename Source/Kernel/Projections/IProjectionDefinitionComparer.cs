// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a system that is capable of comparing <see cref="ProjectionDefinition">projection definitions</see>.
/// </summary>
public interface IProjectionDefinitionComparer
{
    /// <summary>
    /// Compare two <see cref="ProjectionDefinition">projection definitions</see>.
    /// </summary>
    /// <param name="projectionKey">The <see cref="ProjectionKey"/>.</param>
    /// <param name="first">The first <see cref="ProjectionDefinition"/>.</param>
    /// <param name="second">The second <see cref="ProjectionDefinition"/>.</param>
    /// <returns>The <see cref="ProjectionDefinitionCompareResult"/>.</returns>
    Task<ProjectionDefinitionCompareResult> Compare(ProjectionKey projectionKey, ProjectionDefinition first, ProjectionDefinition second);
}
