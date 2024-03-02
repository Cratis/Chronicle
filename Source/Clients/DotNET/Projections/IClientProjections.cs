// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Projections.Definitions;

namespace Aksio.Cratis.Projections;

/// <summary>
/// Defines a system that holds projection definitions.
/// </summary>
public interface IClientProjections
{
    /// <summary>
    /// Gets all the projection definitions.
    /// </summary>
    /// <value>Collection of <see cref="ProjectionDefinition"/>.</value>
    IImmutableList<ProjectionDefinition> Definitions { get; }
}
