// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

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
        IgnoreCasing == other.IgnoreCasing &&
        Scope == other.Scope &&
        EventDefinitions.SequenceEqual(other.EventDefinitions) &&
        RemovedWith.SequenceEqual(other.RemovedWith);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hashCode = default(HashCode);
        hashCode.Add(Name);
        hashCode.Add(IgnoreCasing);
        hashCode.Add(Scope);
        foreach (var eventDefinition in EventDefinitions)
        {
            hashCode.Add(eventDefinition);
        }

        foreach (var removalEventTypeId in RemovedWith)
        {
            hashCode.Add(removalEventTypeId);
        }

        return hashCode.ToHashCode();
    }

    /// <inheritdoc/>
    public ConstraintChange CompareWith(IConstraintDefinition existing)
    {
        if (existing is not UniqueConstraintDefinition existingDefinition)
        {
            return new(true, [ConstraintChangeType.EventAdded, ConstraintChangeType.EventRemoved, ConstraintChangeType.IndexedPropertiesChanged]);
        }

        var changes = new HashSet<ConstraintChangeType>();
        var existingEventDefinitions = existingDefinition.EventDefinitions.ToArray();
        var newEventDefinitions = EventDefinitions.ToArray();

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

        if (!RemovedWith.SequenceEqual(existingDefinition.RemovedWith) || IgnoreCasing != existingDefinition.IgnoreCasing || Scope != existingDefinition.Scope)
        {
            changes.Add(ConstraintChangeType.IndexedPropertiesChanged);
        }

        return changes.Count == 0
            ? ConstraintChange.None
            : new ConstraintChange(true, changes.ToArray());
    }
}
