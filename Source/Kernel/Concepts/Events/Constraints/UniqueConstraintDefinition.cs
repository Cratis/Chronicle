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
    public bool Equals(IConstraintDefinition? other) => base.Equals(other);

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
