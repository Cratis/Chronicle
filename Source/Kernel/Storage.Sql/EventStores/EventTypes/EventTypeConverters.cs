// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using NJsonSchema;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes;

/// <summary>
/// Converter methods for working with <see cref="EventTypeSchema"/> converting to and from SQL representations.
/// </summary>
public static class EventTypeConverters
{
    static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Convert to a <see cref="EventType">SQL</see> representation.
    /// </summary>
    /// <param name="schema"><see cref="EventTypeSchema"/> to convert.</param>
    /// <returns>Converted <see cref="EventType"/>.</returns>
    public static EventType ToSql(this EventTypeSchema schema) =>
        new()
        {
            Id = schema.Type.Id,
            Owner = schema.Owner,
            Source = schema.Source,
            Tombstone = schema.Type.Tombstone,
            Schemas = new Dictionary<uint, string>
            {
                { schema.Type.Generation, schema.Schema.ToJson() }
            }
        };

    /// <summary>
    /// Convert an <see cref="EventTypeDefinition"/> to a <see cref="EventType">SQL</see> representation.
    /// </summary>
    /// <param name="definition"><see cref="EventTypeDefinition"/> to convert.</param>
    /// <returns>Converted <see cref="EventType"/>.</returns>
    public static EventType ToSql(this EventTypeDefinition definition)
    {
        var schemas = new Dictionary<uint, string>();

        foreach (var generation in definition.Generations)
        {
            schemas[(uint)generation.Generation] = generation.Schema.ToJson();
        }

        string migrationsJson;

        try
        {
            migrationsJson = JsonSerializer.Serialize(definition.Migrations, _jsonSerializerOptions);
        }
        catch
        {
            migrationsJson = "[]";
        }

        return new()
        {
            Id = definition.Id,
            Owner = definition.Owner,
            Tombstone = definition.Tombstone,
            Schemas = schemas,
            MigrationsJson = migrationsJson
        };
    }

    /// <summary>
    /// Convert to <see cref="EventTypeSchema"/> from <see cref="EventType"/>.
    /// </summary>
    /// <param name="schema"><see cref="EventType"/> to convert from.</param>
    /// <returns>Converted <see cref="EventTypeSchema"/>.</returns>
    public static EventTypeSchema ToKernel(this EventType schema)
    {
        var result = JsonSchema.FromJsonAsync(schema.Schemas.First().Value).GetAwaiter().GetResult();
        result.EnsureComplianceMetadata();

        return new EventTypeSchema(
            new Concepts.Events.EventType(
               schema.Id,
               EventTypeGeneration.First,
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
            var schema = JsonSchema.FromJsonAsync(kvp.Value).GetAwaiter().GetResult();
            return new EventTypeGenerationDefinition(new EventTypeGeneration(kvp.Key), schema);
        }).ToList();

        IEnumerable<EventTypeMigrationDefinition> migrations = [];

        if (!string.IsNullOrEmpty(eventType.MigrationsJson) && eventType.MigrationsJson != "[]")
        {
            try
            {
                migrations = JsonSerializer.Deserialize<IEnumerable<EventTypeMigrationDefinition>>(
                    eventType.MigrationsJson,
                    _jsonSerializerOptions) ?? [];
            }
            catch
            {
                migrations = [];
            }
        }

        return new EventTypeDefinition(
            eventType.Id,
            eventType.Owner,
            eventType.Tombstone,
            generations,
            migrations);
    }
}

