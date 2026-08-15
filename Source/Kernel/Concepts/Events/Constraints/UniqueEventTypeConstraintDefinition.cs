// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Represents a definition of a unique event type constraint.
/// </summary>
/// <param name="Name">Name of the constraint.</param>
/// <param name="EventTypeIds">The <see cref="EventTypeId"/> values the constraint covers.</param>
/// <param name="RemovedWith">The <see cref="EventTypeId"/> values of the events that release the constraint.</param>
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
/// <para>
/// Several removal events are allowed, because a cycle can end in more than one way — a shift is closed by being
/// ended or by being cancelled. Each of them releases the constraint on its own, so the cycle ends at whichever
/// of them was appended most recently.
/// </para>
/// <para>
/// The primary constructor is named explicitly because the record has two: this one and the obsolete overload
/// taking a single removal event. A serializer offered a choice refuses rather than guesses — the SQL provider
/// persists definitions as JSON and reads them back through this type, and would otherwise throw on the first
/// definition it read.
/// </para>
/// </remarks>
[method: JsonConstructor]
public record UniqueEventTypeConstraintDefinition(ConstraintName Name, IEnumerable<EventTypeId> EventTypeIds, IEnumerable<EventTypeId> RemovedWith = null!, ConstraintScope? Scope = default) : IConstraintDefinition
{
    readonly IEnumerable<EventTypeId>? _eventTypeIds = EventTypeIds;
    readonly IEnumerable<EventTypeId>? _removedWith = RemovedWith;

    /// <summary>
    /// Initializes a new instance of the <see cref="UniqueEventTypeConstraintDefinition"/> class from a single removal event.
    /// </summary>
    /// <param name="name">Name of the constraint.</param>
    /// <param name="eventTypeIds">The <see cref="EventTypeId"/> values the constraint covers.</param>
    /// <param name="removedWith">The <see cref="EventTypeId"/> of the event that releases the constraint, or <see langword="null"/> for none.</param>
    /// <param name="scope">The <see cref="ConstraintScope"/> for the constraint.</param>
    /// <remarks>
    /// The signature this type had while a constraint could only be released by one event. It is kept so that an
    /// assembly compiled against that shape keeps linking: optional arguments are baked in at the call site, so
    /// every previously compiled call refers to the full argument list, which is what this restores.
    /// </remarks>
    [Obsolete("A constraint can be released by more than one event. Pass a collection of event type ids instead - this overload wraps the single value and will be removed.")]
    public UniqueEventTypeConstraintDefinition(
        ConstraintName name,
        IEnumerable<EventTypeId> eventTypeIds,
        EventTypeId? removedWith,
        ConstraintScope? scope)
        : this(name, eventTypeIds, removedWith is null ? [] : [removedWith], scope)
    {
    }

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

    /// <summary>
    /// Gets the <see cref="EventTypeId"/> values of the events that release the constraint.
    /// </summary>
    /// <remarks>
    /// Normalized to an empty sequence when absent, for the same reason and by the same mechanism as
    /// <see cref="EventTypeIds"/>: a constraint that declares no removal event has nothing here, and a definition
    /// persisted before the constraint could be released by several events carries a single value under this name
    /// which storage upgrades on read.
    /// </remarks>
    public IEnumerable<EventTypeId> RemovedWith
    {
        get => _removedWith ?? [];
        init => _removedWith = value;
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
    /// The removal events are part of the comparison because they are part of what the constraint means. Leaving
    /// them out would make adding, changing, or dropping an event that releases the constraint indistinguishable
    /// from a re-registration of the same definition, so the stored definition would keep enforcing the previous
    /// rule. They are compared by content for the same reason the covered event types are.
    /// </para>
    /// </remarks>
    public virtual bool Equals(UniqueEventTypeConstraintDefinition? other) =>
        other is not null &&
        Name == other.Name &&
        Scope == other.Scope &&
        EventTypeIds.SequenceEqual(other.EventTypeIds) &&
        RemovedWith.SequenceEqual(other.RemovedWith);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hashCode = default(HashCode);
        hashCode.Add(Name);
        hashCode.Add(Scope);
        foreach (var eventTypeId in EventTypeIds)
        {
            hashCode.Add(eventTypeId);
        }

        foreach (var removalEventTypeId in RemovedWith)
        {
            hashCode.Add(removalEventTypeId);
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
