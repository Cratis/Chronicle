// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents the state of the <see cref="ProjectionsManager"/>.
/// </summary>
public class ProjectionsManagerState
{
    /// <summary>
    /// Gets or sets the projection definitions.
    /// </summary>
    public IEnumerable<ProjectionDefinition> Projections { get; set; } = [];
}
