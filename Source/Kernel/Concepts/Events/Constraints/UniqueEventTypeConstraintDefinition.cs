// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Represents a definition of a unique event type constraint.
/// </summary>
/// <param name="Name">Name of the constraint.</param>
/// <param name="EventTypeIds">The <see cref="EventTypeId"/> values the constraint covers.</param>
/// <param name="RemovedWith">The <see cref="EventTypeId"/> of the event that releases the constraint.</param>
/// <param name="Scope">The <see cref="ConstraintScope"/> for the constraint.</param>
/// <remarks>
/// The constraint allows at most one event drawn from the covered event types per event source. A single event
/// type expresses "this event happens at most once"; several express mutual exclusion — an event source that is
/// terminal through either of two outcomes can have one of them, never both and never twice.
/// <para>
/// With a removal event the "at most once" is per cycle rather than forever: a covered event violates the
/// constraint only when it comes after the most recent removal event on the same event source. Without one the
/// constraint can only say "forever", which no lifecycle that repeats can express.
/// </para>
/// </remarks>
public record UniqueEventTypeConstraintDefinition(ConstraintName Name, IEnumerable<EventTypeId> EventTypeIds, EventTypeId? RemovedWith = default, ConstraintScope? Scope = default) : IConstraintDefinition
{
    readonly IEnumerable<EventTypeId>? _eventTypeIds = EventTypeIds;

    /// <summary>
    /// Gets the <see cref="EventTypeId"/> values the constraint covers.
    /// </summary>
    /// <remarks>
    /// Normalized to an empty sequence when absent. A definition persisted before the constraint covered several
    /// event types has no value for this at all, and every reader — equality, hashing, validation — would otherwise
    /// dereference null. Storage upgrades such a definition to its single covered event type on read; this is the
    /// backstop for anything that reaches the domain without going through that path.
    /// <para>
    /// The normalization is on the way out rather than in the initializer, because a document deserializer is free
    /// to materialize the record without running either a constructor or the initializer: the MongoDB driver does
    /// exactly that, assigning only the members the document actually carries. An initializer-only guard is then
    /// never reached, and the null it was written to stop reaches every reader.
    /// </para>
    /// </remarks>
    public IEnumerable<EventTypeId> EventTypeIds
    {
        get => _eventTypeIds ?? [];
        init => _eventTypeIds = value;
    }

    /// <inheritdoc/>
    public bool Equals(IConstraintDefinition? other) => Equals(other as UniqueEventTypeConstraintDefinition);

    /// <summary>
    /// Compare with another <see cref="UniqueEventTypeConstraintDefinition"/> by value.
    /// </summary>
    /// <param name="other">The definition to compare with.</param>
    /// <returns>True if the definitions carry the same content, false otherwise.</returns>
    /// <remarks>
    /// The generated record equality would compare the event type ids by reference, and registration decides
    /// whether a constraint changed by comparing the incoming definition to the stored one — so reference
    /// equality would report every re-registration as a change. The collection is therefore compared by content.
    /// <para>
    /// The removal event is part of the comparison because it is part of what the constraint means. Leaving it out
    /// would make adding, changing, or dropping the event that releases the constraint indistinguishable from a
    /// re-registration of the same definition, so the stored definition would keep enforcing the previous rule.
    /// </para>
    /// </remarks>
    public virtual bool Equals(UniqueEventTypeConstraintDefinition? other) =>
        other is not null &&
        Name == other.Name &&
        RemovedWith == other.RemovedWith &&
        Scope == other.Scope &&
        EventTypeIds.SequenceEqual(other.EventTypeIds);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hashCode = default(HashCode);
        hashCode.Add(Name);
        hashCode.Add(RemovedWith);
        hashCode.Add(Scope);
        foreach (var eventTypeId in EventTypeIds)
        {
            hashCode.Add(eventTypeId);
        }

        return hashCode.ToHashCode();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// A change is always detected — by <see cref="Equals(UniqueEventTypeConstraintDefinition)"/>, which registration
    /// uses to decide whether the stored definition is superseded — but never requires reindexing, including when the
    /// removal event changes. This constraint keeps no index: it is enforced by reading the appended events for the
    /// event source, so the next append answers against the new definition with nothing to rebuild first.
    /// </remarks>
    public ConstraintChange CompareWith(IConstraintDefinition existing) => ConstraintChange.None;
}
