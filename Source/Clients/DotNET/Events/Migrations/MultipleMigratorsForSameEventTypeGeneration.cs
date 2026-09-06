// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// The exception that is thrown when more than one migrator defines the migration between the same
/// pair of generations for an event type.
/// </summary>
/// <param name="eventType">The <see cref="EventType"/> the migrators target.</param>
/// <param name="from">The <see cref="EventTypeGeneration"/> migrated from.</param>
/// <param name="to">The <see cref="EventTypeGeneration"/> migrated to.</param>
/// <param name="migratorTypes">The CLR types of the conflicting migrators.</param>
public class MultipleMigratorsForSameEventTypeGeneration(EventType eventType, EventTypeGeneration from, EventTypeGeneration to, IEnumerable<Type> migratorTypes)
    : Exception($"Multiple migrators define the migration from generation {from} to generation {to} of event type '{eventType.Id}': {string.Join(", ", migratorTypes.Select(_ => _.Name))}. Exactly one migrator may bridge a pair of generations.")
{
    /// <summary>
    /// Gets the <see cref="EventType"/> the conflicting migrators target.
    /// </summary>
    public EventType EventType { get; } = eventType;
}
