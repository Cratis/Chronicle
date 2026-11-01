// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using KernelConcepts::Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Stores a kernel seed as immutable values; no JSON node or definition escapes into another store.
/// </summary>
internal sealed class EventTypeSeed
{
    readonly FrozenDictionary<EventTypeId, Definition> _definitions;

    /// <summary>
    /// Captures discovered definitions and their registration metadata.
    /// </summary>
    /// <param name="definitions">The registration templates.</param>
    internal EventTypeSeed(IEnumerable<EventTypeToRegister> definitions) =>
        _definitions = definitions.ToFrozenDictionary(item => item.Definition.Id, item => new Definition(
            item.Definition.Id,
            item.Definition.Owner,
            item.Definition.Tombstone,
            item.Source,
            item.Visibility,
            item.Origin,
            item.Definition.Generations.Select(generation => new Generation(generation.Generation, generation.Schema.ToJson())).ToImmutableArray(),
            item.Definition.Migrations.Select(migration => new Migration(migration.FromGeneration, migration.ToGeneration, migration.UpcastJmesPath.ToJsonString(), migration.DowncastJmesPath.ToJsonString())).ToImmutableArray()));

    /// <summary>
    /// Gets the identifiers in the seed.
    /// </summary>
    internal IEnumerable<EventTypeId> Ids => _definitions.Keys;

    /// <summary>
    /// Checks whether an identifier and optional generation are present.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="generation">The optional generation.</param>
    /// <returns>Whether the seed contains the requested schema.</returns>
    internal bool Has(EventTypeId id, EventTypeGeneration? generation = null) =>
        _definitions.TryGetValue(id, out var definition) && (generation is null || definition.Generations.Any(item => item.Number == generation));

    /// <summary>
    /// Materializes a definition exclusively for the caller's store.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>A fresh registration.</returns>
    internal EventTypeToRegister Materialize(EventTypeId id)
    {
        var definition = _definitions[id];
        var generations = definition.Generations.Select(generation => new EventTypeGenerationDefinition(generation.Number, JsonSchema.FromJson(generation.Schema))).ToArray();
        var migrations = definition.Migrations.Select(migration => new EventTypeMigrationDefinition(migration.From, migration.To, [], JsonNode.Parse(migration.Upcast)!.AsObject(), JsonNode.Parse(migration.Downcast)!.AsObject())).ToArray();
        return new(new EventTypeDefinition(id, definition.Owner, definition.Tombstone, generations, migrations), definition.Source, definition.Visibility, definition.Origin);
    }

    sealed record Definition(EventTypeId Id, EventTypeOwner Owner, bool Tombstone, EventTypeSource Source, EventTypeVisibility Visibility, string Origin, ImmutableArray<Generation> Generations, ImmutableArray<Migration> Migrations);
    sealed record Generation(EventTypeGeneration Number, string Schema);
    sealed record Migration(EventTypeGeneration From, EventTypeGeneration To, string Upcast, string Downcast);
}
