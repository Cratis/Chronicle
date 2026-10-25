// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.Contracts.EventSources;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Represents the prepared wire definitions used to compute an artifact fingerprint.
/// </summary>
/// <param name="EventTypes">The event types.</param>
/// <param name="EventSources">The event sources.</param>
/// <param name="ReadModels">The read models.</param>
/// <param name="Constraints">The constraints.</param>
/// <param name="Projections">The projections.</param>
internal record ArtifactDefinitionPayloads(
    IEnumerable<Contracts.Events.EventTypeRegistration> EventTypes,
    IEnumerable<EventSourceDefinition> EventSources,
    IEnumerable<ReadModelDefinition> ReadModels,
    IEnumerable<Constraint> Constraints,
    IEnumerable<ProjectionDefinition> Projections)
{
    /// <summary>
    /// Gets an empty set of definitions.
    /// </summary>
    public static readonly ArtifactDefinitionPayloads Empty = new([], [], [], [], []);
}
