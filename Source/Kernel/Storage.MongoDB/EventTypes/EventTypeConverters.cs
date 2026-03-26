// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Events.EventTypes;

/// <summary>
/// Converter methods for working with <see cref="EventTypeSchema"/> converting to and from MongoDB representations.
/// </summary>
public static class EventTypeConverters
{
    /// <summary>
    /// Convert to a <see cref="EventType">MongoDB</see> representation.
    /// </summary>
    /// <param name="schema"><see cref="EventTypeSchema"/> to convert.</param>
    /// <returns>Converted <see cref="EventType"/>.</returns>
    public static EventType ToMongoDB(this EventTypeSchema schema)
    {
        return new EventType(
            schema.Type.Id,
            schema.Owner,
            schema.Source,
            schema.Type.Tombstone,
            new Dictionary<string, BsonDocument>
            {
                { schema.Type.Generation.ToString(), BsonDocument.Parse(schema.Schema.ToJson()) }
            });
    }

    /// <summary>
    /// Convert a full <see cref="EventTypeDefinition"/> to a <see cref="EventType">MongoDB</see> representation.
    /// </summary>
    /// <param name="definition">The <see cref="EventTypeDefinition"/> to convert.</param>
    /// <param name="owner">The <see cref="EventTypeOwner"/>.</param>
    /// <param name="source">The <see cref="EventTypeSource"/>.</param>
    /// <returns>Converted <see cref="EventType"/>.</returns>
    public static EventType ToMongoDB(this EventTypeDefinition definition, EventTypeOwner owner = EventTypeOwner.Client, EventTypeSource source = EventTypeSource.Code)
    {
        var schemas = definition.Generations.ToDictionary(
            g => g.Generation.ToString(),
            g => BsonDocument.Parse(g.Schema.ToJson()));

        var migrations = definition.Migrations.Select(m => new EventTypeMigration(
            m.FromGeneration,
            m.ToGeneration,
            BsonDocument.Parse(m.UpcastJmesPath?.ToJsonString() ?? "{}"),
            BsonDocument.Parse(m.DowncastJmesPath?.ToJsonString() ?? "{}"))).ToList();

        return new EventType(
            definition.Id,
            owner,
            source,
            definition.Tombstone,
            schemas,
            migrations);
    }

    /// <summary>
    /// Convert to <see cref="EventTypeSchema"/> from <see cref="EventType"/>.
    /// </summary>
    /// <param name="schema"><see cref="EventType"/> to convert from.</param>
    /// <param name="generation">Optional specific <see cref="EventTypeGeneration"/> to use. Defaults to the first generation.</param>
    /// <returns>Converted <see cref="EventTypeSchema"/>.</returns>
    public static EventTypeSchema ToKernel(this EventType schema, EventTypeGeneration? generation = null)
    {
        generation ??= EventTypeGeneration.First;
        var generationKey = generation.ToString();

        BsonDocument schemaBson;
        if (schema.Schemas.TryGetValue(generationKey, out var specificSchema))
        {
            schemaBson = specificSchema;
        }
        else
        {
            schemaBson = schema.Schemas.First().Value;
        }

        var result = JsonSchema.FromJsonAsync(schemaBson.ToJson()).GetAwaiter().GetResult();
        result.EnsureComplianceMetadata();

        return new EventTypeSchema(
            new Concepts.Events.EventType(
               schema.Id,
               generation,
               schema.Tombstone),
            schema.Owner,
            schema.Source,
            result);
    }

    /// <summary>
    /// Convert to <see cref="EventTypeDefinition"/> from <see cref="EventType"/>.
    /// </summary>
    /// <param name="eventType"><see cref="EventType"/> to convert from.</param>
    /// <returns>Converted <see cref="EventTypeDefinition"/>.</returns>
    public static EventTypeDefinition ToDefinition(this EventType eventType)
    {
        var generations = eventType.Schemas.Select(kvp =>
        {
            var generation = new EventTypeGeneration(uint.Parse(kvp.Key));
            var schema = JsonSchema.FromJsonAsync(kvp.Value.ToJson()).GetAwaiter().GetResult();
            schema.EnsureComplianceMetadata();
            return new EventTypeGenerationDefinition(generation, schema);
        }).ToList();

        var migrations = (eventType.Migrations ?? []).Select(m =>
        {
            var upcastJson = JsonNode.Parse(m.UpcastJmesPath.ToJson())?.AsObject() ?? new JsonObject();
            var downcastJson = JsonNode.Parse(m.DowncastJmesPath.ToJson())?.AsObject() ?? new JsonObject();
            return new EventTypeMigrationDefinition(m.FromGeneration, m.ToGeneration, [], upcastJson, downcastJson);
        }).ToList();

        return new EventTypeDefinition(
            eventType.Id,
            eventType.Owner,
            eventType.Tombstone,
            generations,
            migrations);
    }
}
