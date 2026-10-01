// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Concepts.Alerts;

/// <summary>
/// Represents the unique identifier of an alert incident.
/// </summary>
/// <remarks>
/// An incident is one run of a problem from the moment it is raised to the moment it is cleared. A partition incident
/// reuses the identifier of the <see cref="FailedPartition"/> it is about, which is stored, stable across restarts and
/// new for each failure episode. An observer quarantine incident gets a new identifier from <see cref="New"/>.
/// </remarks>
/// <param name="Value">The inner value.</param>
public record IncidentId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Implicitly convert from a <see cref="Guid"/> to <see cref="IncidentId"/>.
    /// </summary>
    /// <param name="value"><see cref="Guid"/> to convert from.</param>
    public static implicit operator IncidentId(Guid value) => new(value);

    /// <summary>
    /// Implicitly convert from a <see cref="FailedPartitionId"/> to <see cref="IncidentId"/>, so that a failed partition
    /// and the incident raised for it share an identity.
    /// </summary>
    /// <param name="id"><see cref="FailedPartitionId"/> to convert from.</param>
    public static implicit operator IncidentId(FailedPartitionId id) => new(id.Value);

    /// <summary>
    /// Creates a new <see cref="IncidentId"/> with a new unique value.
    /// </summary>
    /// <returns>A new <see cref="IncidentId"/>.</returns>
    public static IncidentId New() => new(Guid.NewGuid());
}
