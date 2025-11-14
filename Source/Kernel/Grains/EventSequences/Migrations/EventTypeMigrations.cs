// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage.EventTypes;
using JsonCons.JmesPath;

namespace Cratis.Chronicle.Grains.EventSequences.Migrations;

/// <summary>
/// Represents an implementation of <see cref="IEventTypeMigrations"/>.
/// </summary>
/// <param name="eventTypesStorage">The <see cref="IEventTypesStorage"/> for retrieving event type definitions.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting between JSON and ExpandoObject.</param>
public class EventTypeMigrations(
    IEventTypesStorage eventTypesStorage,
    IExpandoObjectConverter expandoObjectConverter) : IEventTypeMigrations
{
    /// <inheritdoc/>
    public async Task<IDictionary<EventTypeGeneration, ExpandoObject>> MigrateToAllGenerations(EventType eventType, JsonObject content)
    {
        var result = new Dictionary<EventTypeGeneration, ExpandoObject>();

        // Get the event type definition with all generations and migrations
        var definition = await eventTypesStorage.GetDefinition(eventType.Id);

        // If there's only one generation, just convert and return
        if (!definition.Generations.Skip(1).Any())
        {
            var schema = definition.Generations.First().Schema;
            result[eventType.Generation] = expandoObjectConverter.ToExpandoObject(content, schema);
            return result;
        }

        // Add the source generation
        var sourceGenerationDef = definition.Generations.First(g => g.Generation == eventType.Generation);
        result[eventType.Generation] = expandoObjectConverter.ToExpandoObject(content, sourceGenerationDef.Schema);

        // Upcast to higher generations
        await UpcastToHigherGenerations(eventType.Generation, content, definition, result);

        // Downcast to lower generations
        await DowncastToLowerGenerations(eventType.Generation, content, definition, result);

        return result;
    }

    async Task UpcastToHigherGenerations(
        EventTypeGeneration sourceGeneration,
        JsonObject sourceContent,
        EventTypeDefinition definition,
        Dictionary<EventTypeGeneration, ExpandoObject> result)
    {
        var currentContent = sourceContent;
        var currentGeneration = sourceGeneration;

        // Get all migrations that upcast from the source generation
        var migrations = definition.Migrations
            .Where(m => m.FromGeneration.Value >= currentGeneration.Value)
            .OrderBy(m => m.FromGeneration.Value)
            .ToList();

        foreach (var migration in migrations)
        {
            if (migration.FromGeneration == currentGeneration)
            {
                // Apply the upcast migration
                currentContent = ApplyMigration(currentContent, migration);
                currentGeneration = migration.ToGeneration;

                var targetGenerationDef = definition.Generations.First(g => g.Generation == currentGeneration);
                result[currentGeneration] = expandoObjectConverter.ToExpandoObject(currentContent, targetGenerationDef.Schema);
            }
        }

        await Task.CompletedTask;
    }

    async Task DowncastToLowerGenerations(
        EventTypeGeneration sourceGeneration,
        JsonObject sourceContent,
        EventTypeDefinition definition,
        Dictionary<EventTypeGeneration, ExpandoObject> result)
    {
        var currentContent = sourceContent;
        var currentGeneration = sourceGeneration;

        // Get all migrations that downcast from the source generation
        var migrations = definition.Migrations
            .Where(m => m.ToGeneration.Value <= currentGeneration.Value)
            .OrderByDescending(m => m.ToGeneration.Value)
            .ToList();

        foreach (var migration in migrations)
        {
            if (migration.ToGeneration == currentGeneration)
            {
                // Apply the downcast migration (reverse direction)
                currentContent = ApplyMigration(currentContent, migration);
                currentGeneration = migration.FromGeneration;

                var targetGenerationDef = definition.Generations.First(g => g.Generation == currentGeneration);
                result[currentGeneration] = expandoObjectConverter.ToExpandoObject(currentContent, targetGenerationDef.Schema);
            }
        }

        await Task.CompletedTask;
    }

    JsonObject ApplyMigration(JsonObject content, EventTypeMigrationDefinition migration)
    {
        // Apply JmesPath transformation if defined
        if (migration.JmesPath?.Count > 0)
        {
            try
            {
                // The JmesPath expression defines the transformation
                var jmesPathExpr = migration.JmesPath.ToJsonString();

                // Convert JsonObject to JsonElement for JsonTransformer
                var contentJson = content.ToJsonString();
                using var contentDoc = JsonDocument.Parse(contentJson);

                // Apply the JmesPath transformation using JsonCons.JmesPath
                using var resultDoc = JsonTransformer.Transform(contentDoc.RootElement, jmesPathExpr);

                // Convert result back to JsonObject
                var resultJson = JsonSerializer.Serialize(resultDoc.RootElement);
                return JsonNode.Parse(resultJson)?.AsObject() ?? new JsonObject();
            }
            catch
            {
                // If transformation fails, return original content
                return content;
            }
        }

        return content;
    }
}