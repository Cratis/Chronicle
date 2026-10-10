// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Defines the events that close and reopen owned stream scopes.
/// </summary>
/// <param name="Name">The owning constraint name.</param>
/// <param name="EventTypeIds">The closing event types.</param>
/// <param name="Dimensions">The participating scope dimensions.</param>
/// <param name="ReopenedBy">The reopening event types.</param>
/// <param name="EventStreamIdFrom">The optional payload property providing the stream identifier.</param>
/// <remarks>
/// Registration is not retroactive. Closures are written after a durable append in the same grain turn,
/// but are not crash-atomic with the event write. An explicitly requested reindex can repair that window.
/// Removing the declaration or redacting a closing event does not remove its persisted closures.
/// If a rebuild cannot recover a redacted property-sourced closing or reopening scope from an existing
/// closure snapshot, it leaves this owner's rows untouched, logs a warning and rebuilds other constraints.
/// </remarks>
[method: JsonConstructor]
public record ClosesStreamConstraintDefinition(
    ConstraintName Name,
    IEnumerable<EventTypeId> EventTypeIds,
    ClosedStreamDimensions Dimensions,
    IEnumerable<EventTypeId> ReopenedBy,
    string? EventStreamIdFrom = default) : IConstraintDefinition
{
    /// <summary>
    /// Gets the event sequences this declaration applies to; empty means all sequences.
    /// </summary>
    public IEnumerable<EventSequenceId> EventSequences { get; init; } = [];

    /// <inheritdoc/>
    public bool AppliesTo(EventSequenceId eventSequenceId) => EventSequences.Covers(eventSequenceId);

    /// <inheritdoc/>
    public bool Equals(IConstraintDefinition? other) => Equals(other as ClosesStreamConstraintDefinition);

    /// <summary>
    /// Compare closing declarations by content rather than collection identity.
    /// </summary>
    /// <param name="other">The declaration to compare.</param>
    /// <returns>True if the declarations have equal content.</returns>
    public virtual bool Equals(ClosesStreamConstraintDefinition? other) =>
        other is not null && Name == other.Name && Dimensions == other.Dimensions &&
        EventStreamIdFrom == other.EventStreamIdFrom && EventTypeIds.SequenceEqual(other.EventTypeIds) &&
        ReopenedBy.SequenceEqual(other.ReopenedBy) && EventSequences.CoversSameAs(other.EventSequences);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(Name);
        hash.Add(Dimensions);
        hash.Add(EventStreamIdFrom);
        foreach (var eventType in EventTypeIds)
        {
            hash.Add(eventType);
        }

        foreach (var eventType in ReopenedBy)
        {
            hash.Add(eventType);
        }

        hash.AddEventSequences(EventSequences);

        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Changes never request automatic reindexing: historical events close nothing merely because a declaration changes.
    /// </remarks>
    public ConstraintChange CompareWith(IConstraintDefinition existing) => ConstraintChange.None;
}
