// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents the state of the <see cref="ProjectionsManager"/>.
/// </summary>
public class ProjectionsManagerState
{
    /// <summary>
    /// Gets or sets the projection definitions.
    /// </summary>
    public IEnumerable<ProjectionDefinition> Projections { get; set; } = [];

    /// <summary>
    /// Gets or sets the registrant that last registered each projection, keyed by projection identifier.
    /// </summary>
    /// <remarks>
    /// Several applications share one event store and each registers its own full set. Retirement is scoped to the
    /// registrant so one application's full set never retires another's projections.
    /// </remarks>
    public IDictionary<string, string> Registrants { get; set; } = new Dictionary<string, string>();
}
