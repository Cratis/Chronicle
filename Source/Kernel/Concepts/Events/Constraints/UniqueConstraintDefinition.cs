// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Represents a definition of a unique event type constraint.
/// </summary>
/// <param name="Name">Name of the constraint.</param>
/// <param name="EventDefinitions">Collection of <see cref="UniqueConstraintEventDefinition"/>.</param>
/// <param name="RemovedWith">The <see cref="EventTypeId"/> of the event that removes the constraint.</param>
/// <param name="IgnoreCasing">Whether this constraint should ignore casing.</param>
/// <param name="Scope">The <see cref="ConstraintScope"/> for the constraint.</param>
public record UniqueConstraintDefinition(ConstraintName Name, IEnumerable<UniqueConstraintEventDefinition> EventDefinitions, EventTypeId? RemovedWith = default, bool IgnoreCasing = false, ConstraintScope? Scope = default) : IConstraintDefinition
{
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
        RemovedWith == other.RemovedWith &&
        IgnoreCasing == other.IgnoreCasing &&
        Scope == other.Scope &&
        EventDefinitions.SequenceEqual(other.EventDefinitions);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hashCode = default(HashCode);
        hashCode.Add(Name);
        hashCode.Add(RemovedWith);
        hashCode.Add(IgnoreCasing);
        hashCode.Add(Scope);
        foreach (var eventDefinition in EventDefinitions)
        {
            hashCode.Add(eventDefinition);
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

        if (RemovedWith != existingDefinition.RemovedWith || IgnoreCasing != existingDefinition.IgnoreCasing || Scope != existingDefinition.Scope)
        {
            changes.Add(ConstraintChangeType.IndexedPropertiesChanged);
        }

        return changes.Count == 0
            ? ConstraintChange.None
            : new ConstraintChange(true, changes.ToArray());
    }
}
