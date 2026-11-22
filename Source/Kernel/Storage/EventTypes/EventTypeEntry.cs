// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.EventTypes;

/// <summary>
/// Everything known about one event type in an <see cref="EventTypeRegistrySnapshot"/>: its latest schema, every
/// generation, owner, source, visibility, tombstone flag and migrations.
/// </summary>
/// <remarks>
/// An entry is immutable and shared. There is exactly one parsed schema per generation, and the
/// <see cref="EventTypeSchema"/> handed out for a generation is the same instance for every reader. Values derived
/// from those instances can hang off them through weak tables: a changed event type gets a new entry with new
/// instances, so anything derived from the old ones is recomputed and the old values go away with them.
/// </remarks>
public sealed class EventTypeEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypeEntry"/> class.
    /// </summary>
    /// <param name="definition">The <see cref="EventTypeDefinition"/> with every generation and migration.</param>
    /// <param name="latest">The <see cref="EventTypeSchema"/> of the latest generation, which carries source, visibility and origin.</param>
    public EventTypeEntry(EventTypeDefinition definition, EventTypeSchema latest)
    {
        var generations = definition.Generations.ToArray();
        Definition = definition with
        {
            Generations = generations,
            Migrations = definition.Migrations.ToArray()
        };
        Latest = latest;
        Generations = generations.ToFrozenDictionary(
            _ => _.Generation,
            _ => _.Generation == latest.Type.Generation && ReferenceEquals(_.Schema, latest.Schema)
                ? latest
                : latest with { Type = latest.Type with { Generation = _.Generation }, Schema = _.Schema });
    }

    /// <summary>
    /// Gets the <see cref="EventTypeId"/> of the event type.
    /// </summary>
    public EventTypeId Id => Definition.Id;

    /// <summary>
    /// Gets the <see cref="EventTypeDefinition"/> - generations, migrations, owner and tombstone.
    /// </summary>
    public EventTypeDefinition Definition { get; }

    /// <summary>
    /// Gets the schema of the latest generation.
    /// </summary>
    public EventTypeSchema Latest { get; }

    /// <summary>
    /// Gets the schema of every generation, keyed by generation.
    /// </summary>
    public IReadOnlyDictionary<EventTypeGeneration, EventTypeSchema> Generations { get; }

    /// <summary>
    /// Gets whether another entry describes exactly the same event type, so a reload can keep this one by reference.
    /// </summary>
    /// <param name="other">The <see cref="EventTypeEntry"/> to compare with.</param>
    /// <returns>True if nothing a reader can observe differs, false otherwise.</returns>
    public bool IsSameAs(EventTypeEntry other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        var left = Definition;
        var right = other.Definition;
        if (left.Id != right.Id ||
            left.Owner != right.Owner ||
            left.Tombstone != right.Tombstone ||
            Latest.Source != other.Latest.Source ||
            Latest.Visibility != other.Latest.Visibility ||
            Latest.Origin != other.Latest.Origin ||
            Latest.Type.Generation != other.Latest.Type.Generation)
        {
            return false;
        }

        var leftGenerations = left.Generations.ToArray();
        var rightGenerations = right.Generations.ToDictionary(_ => _.Generation);
        if (leftGenerations.Length != rightGenerations.Count ||
            leftGenerations.Any(_ => !rightGenerations.TryGetValue(_.Generation, out var generation) || _.Schema.ToJson() != generation.Schema.ToJson()))
        {
            return false;
        }

        var leftMigrations = left.Migrations.ToArray();
        var rightMigrations = right.Migrations.ToDictionary(_ => (_.FromGeneration, _.ToGeneration));
        return leftMigrations.Length == rightMigrations.Count &&
            leftMigrations.All(_ => rightMigrations.TryGetValue((_.FromGeneration, _.ToGeneration), out var migration) &&
                JsonNode.DeepEquals(_.UpcastJmesPath, migration.UpcastJmesPath) &&
                JsonNode.DeepEquals(_.DowncastJmesPath, migration.DowncastJmesPath));
    }
}
