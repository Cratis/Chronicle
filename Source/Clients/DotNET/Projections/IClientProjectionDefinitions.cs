// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Projections.Definitions;

namespace Aksio.Cratis.Projections;

/// <summary>
/// Defines a system that holds projection definitions.
/// </summary>
public interface IClientProjectionDefinitions
{
    /// <summary>
    /// Gets all the projection definitions.
    /// </summary>
    /// <value>Collection of <see cref="ProjectionDefinition"/>.</value>
    IEnumerable<ProjectionDefinition> All { get; }
}
