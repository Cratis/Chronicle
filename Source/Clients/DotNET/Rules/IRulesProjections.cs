// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Projections.Definitions;

namespace Aksio.Cratis.Rules;

/// <summary>
/// Defines a system that knows about projections related to rules.
/// </summary>
public interface IRulesProjections
{
    /// <summary>
    /// Gets all the projection definitions.
    /// </summary>
    /// <value>Collection of <see cref="ProjectionDefinition"/>.</value>
    IEnumerable<ProjectionDefinition> All { get; }
}
