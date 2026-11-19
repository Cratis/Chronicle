// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Represents a definition of a unique event type constraint.
/// </summary>
/// <param name="Name">Name of the constraint.</param>
/// <param name="EventDefinitions">Collection of <see cref="UniqueConstraintEventDefinition"/>.</param>
/// <param name="RemovedWith">The <see cref="EventTypeId"/> values of the events that remove the constraint.</param>
/// <param name="IgnoreCasing">Whether this constraint should ignore casing.</param>
/// <param name="Scope">The <see cref="ConstraintScope"/> for the constraint.</param>
/// <remarks>
/// Several removal events are allowed, because a lifecycle can end in more than one way — an invited address is
/// released by the invitation being accepted, revoked or expiring. Each of them releases the claimed value on its
/// own, so the value is free again after whichever of them the event source reaches.
/// <para>
/// The primary constructor is named explicitly because the record has two: this one and the obsolete overload
/// taking a single removal event. A serializer offered a choice refuses rather than guesses — the SQL provider
/// persists definitions as JSON and reads them back through this type, and would otherwise throw on the first
/// definition it read.
/// </para>
/// </remarks>
[method: JsonConstructor]
public record UniqueConstraintDefinition(ConstraintName Name, IEnumerable<UniqueConstraintEventDefinition> EventDefinitions, IEnumerable<EventTypeId> RemovedWith = null!, bool IgnoreCasing = false, ConstraintScope? Scope = default) : IConstraintDefinition
{
    readonly IEnumerable<EventTypeId>? _removedWith = RemovedWith;
    readonly IEnumerable<EventSequenceId>? _eventSequences;
    readonly IEnumerable<UniqueConstraintEventDefinition>? _removalEventDefinitions;

    /// <summary>
    /// Initializes a new instance of the <see cref="UniqueConstraintDefinition"/> class from a single removal event.
    /// </summary>
    /// <param name="name">Name of the constraint.</param>
    /// <param name="eventDefinitions">Collection of <see cref="UniqueConstraintEventDefinition"/>.</param>
    /// <param name="removedWith">The <see cref="EventTypeId"/> of the event that removes the constraint, or <see langword="null"/> for none.</param>
    /// <param name="ignoreCasing">Whether this constraint should ignore casing.</param>
    /// <param name="scope">The <see cref="ConstraintScope"/> for the constraint.</param>
    /// <remarks>
    /// The signature this type had while a constraint could only be released by one event. It is kept so that an
    /// assembly compiled against that shape keeps linking: optional arguments are baked in at the call site, so
    /// every previously compiled call refers to the full argument list, which is what this restores.
    /// </remarks>
    [Obsolete("A constraint can be released by more than one event. Pass a collection of event type ids instead - this overload wraps the single value and will be removed.")]
    public UniqueConstraintDefinition(
        ConstraintName name,
        IEnumerable<UniqueConstraintEventDefinition> eventDefinitions,
        EventTypeId? removedWith,
        bool ignoreCasing,
        ConstraintScope? scope)
        : this(name, eventDefinitions, removedWith is null ? [] : [removedWith], ignoreCasing, scope)
    {
    }

    /// <summary>
    /// Gets how claimed values are retained. The default keeps one value per event source.
    /// </summary>
    public UniqueConstraintMode Mode { get; init; }

    /// <summary>
    /// Gets the removal events and properties identifying the value to release.
    /// </summary>
    /// <remarks>
    /// A removal event without properties releases all values held by its event source.
    /// </remarks>
    public IEnumerable<UniqueConstraintEventDefinition> RemovalEventDefinitions
    {
        get => _removalEventDefinitions ?? [];
        init => _removalEventDefinitions = value;
    }

    /// <summary>
    /// Gets the <see cref="EventTypeId"/> values of the events that remove the constraint.
    /// </summary>
    /// <remarks>
    /// Normalized to an empty sequence when absent. Most constraints declare no removal event at all, and a
    /// definition persisted before the constraint could be released by several events carries a single value under
    /// this name which storage upgrades on read. The normalization is on the way out rather than in the
    /// initializer, because a document deserializer is free to materialize the record without running either a
    /// constructor or the initializer — the MongoDB driver does exactly that.
    /// </remarks>
    public IEnumerable<EventTypeId> RemovedWith
    {
        get => _removedWith ?? [];
        init => _removedWith = value;
    }

    /// <summary>
    /// Gets the <see cref="EventSequenceId"/> values of the event sequences the constraint applies to.
    /// </summary>
    /// <remarks>
    /// Empty means every event sequence, which is the default. The constraint is neither validated nor indexed for an
    /// event sequence it does not apply to - uniqueness is typically a rule of the event log, and an outbox that
    /// republishes already-validated facts must not keep an index of its own that only removal events appended to the
    /// outbox could release.
    /// <para>
    /// It is an init-only member rather than a constructor parameter so that assemblies compiled against the existing
    /// constructors keep linking. It is normalized on the way out for the same reason as <see cref="RemovedWith"/>: a
    /// definition persisted before the member existed does not carry it, and a document deserializer does not run the
    /// initializer.
    /// </para>
    /// </remarks>
    public IEnumerable<EventSequenceId> EventSequences
    {
        get => _eventSequences ?? [];
        init => _eventSequences = value;
    }

    /// <inheritdoc/>
    public bool AppliesTo(EventSequenceId eventSequenceId) => EventSequences.Covers(eventSequenceId);

    /// <inheritdoc/>
    public bool Equals(IConstraintDefinition? other) => Equals(other as UniqueConstraintDefinition);

    /// <summary>
    /// Compare with another <see cref="UniqueConstraintDefinition"/> by value.
    /// </summary>
    /// <param name="other">The definition to compare with.</param>
    /// <returns>True if the definitions carry the same content, false otherwise.</returns>
    /// <remarks>
    /// The generated record equality would compare the covered events by reference, and registration decides
    /// whether a constraint changed by comparing the incoming definition to the stored one - so reference
    /// equality reported every re-registration as a change. The incoming definition is rebuilt from the client's
    /// attributes on every connect, so that was every startup: another version of an identical definition
    /// persisted, and a reindex asked for, each time the process came up. The sibling unique-event-type
    /// definition already compares its covered event types by content for exactly this reason.
    /// </remarks>
    public virtual bool Equals(UniqueConstraintDefinition? other) =>
        other is not null &&
        Name == other.Name &&
        !ClaimsDifferFrom(other) &&
        EventDefinitions.Canonical().SequenceEqual(other.EventDefinitions.Canonical()) &&
        EventSequences.CoversSameAs(other.EventSequences);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hashCode = default(HashCode);
        hashCode.Add(Name);
        hashCode.Add(IgnoreCasing);
        hashCode.Add(Mode);
        foreach (var removalEventDefinition in RemovalEventDefinitions.Canonical())
        {
            hashCode.Add(removalEventDefinition);
        }

        hashCode.Add(Scope.Canonical());
        foreach (var eventDefinition in EventDefinitions.Canonical())
        {
            hashCode.Add(eventDefinition);
        }

        foreach (var removalEventTypeId in RemovedWith.Canonical())
        {
            hashCode.Add(removalEventTypeId);
        }

        hashCode.AddEventSequences(EventSequences);
        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Get this definition in its canonical form, see <see cref="ConstraintNormalization"/>.
    /// </summary>
    /// <returns>An equal definition whose collections are distinct and ordered, and whose empty scope is absent.</returns>
    public UniqueConstraintDefinition Normalized() => this with
    {
        EventDefinitions = EventDefinitions.Canonical(),
        RemovedWith = RemovedWith.Canonical(),
        RemovalEventDefinitions = RemovalEventDefinitions.Canonical(),
        EventSequences = EventSequences.Canonical(),
        Scope = Scope.Canonical()
    };

    /// <inheritdoc/>
    public ConstraintChange CompareWith(IConstraintDefinition existing)
    {
        if (existing is not UniqueConstraintDefinition existingDefinition)
        {
            return new(true, [ConstraintChangeType.EventAdded, ConstraintChangeType.EventRemoved, ConstraintChangeType.IndexedPropertiesChanged]);
        }

        var changes = new HashSet<ConstraintChangeType>();
        CollectEventChanges(existingDefinition, changes);

        if (ClaimsDifferFrom(existingDefinition))
        {
            changes.Add(ConstraintChangeType.IndexedPropertiesChanged);
        }

        // Narrowing needs no reindex: only newly covered sequences need their indexes rebuilt.
        if (EventSequences.CoversMoreThan(existingDefinition.EventSequences))
        {
            changes.Add(ConstraintChangeType.EventSequencesChanged);
        }

        return changes.Count == 0
            ? ConstraintChange.None
            : new ConstraintChange(true, changes.ToArray());
    }

    void CollectEventChanges(UniqueConstraintDefinition existingDefinition, HashSet<ConstraintChangeType> changes)
    {
        var existingEventDefinitions = existingDefinition.EventDefinitions.Canonical();
        var newEventDefinitions = EventDefinitions.Canonical();

        var existingEventTypes = existingEventDefinitions.Select(_ => _.EventTypeId).ToHashSet();
        var newEventTypes = newEventDefinitions.Select(_ => _.EventTypeId).ToHashSet();

        if (newEventTypes.Except(existingEventTypes).Any())
        {
            changes.Add(ConstraintChangeType.EventAdded);
        }

        if (existingEventTypes.Except(newEventTypes).Any())
        {
            changes.Add(ConstraintChangeType.EventRemoved);
        }

        foreach (var eventType in existingEventTypes.Intersect(newEventTypes).ToArray())
        {
            var existingForType = existingEventDefinitions.Where(_ => _.EventTypeId == eventType).ToArray();
            var newForType = newEventDefinitions.Where(_ => _.EventTypeId == eventType).ToArray();

            if (!existingForType.SequenceEqual(newForType))
            {
                changes.Add(ConstraintChangeType.IndexedPropertiesChanged);
                break;
            }
        }
    }

    bool ClaimsDifferFrom(UniqueConstraintDefinition existingDefinition) =>
        !RemovedWith.SameSetAs(existingDefinition.RemovedWith) ||
        !RemovalEventDefinitions.Canonical().SequenceEqual(existingDefinition.RemovalEventDefinitions.Canonical()) ||
        Mode != existingDefinition.Mode ||
        IgnoreCasing != existingDefinition.IgnoreCasing ||
        Scope.Canonical() != existingDefinition.Scope.Canonical();
}
