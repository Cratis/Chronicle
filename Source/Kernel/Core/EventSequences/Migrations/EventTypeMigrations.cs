// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage;
using JsonCons.JmesPath;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Represents an implementation of <see cref="IEventTypeMigrations"/>.
/// </summary>
/// <param name="storage">The <see cref="IStorage"/> for accessing the underlying storage.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting between JSON and ExpandoObject.</param>
public class EventTypeMigrations(
    IStorage storage,
    IExpandoObjectConverter expandoObjectConverter) : IEventTypeMigrations
{
    /// <inheritdoc/>
    public async Task<IDictionary<EventTypeGeneration, ExpandoObject>> MigrateToAllGenerations(EventStoreName eventStore, EventType eventType, JsonObject content, ExpandoObject contentAsExpandoObject)
    {
        var result = new Dictionary<EventTypeGeneration, ExpandoObject>();
        var eventTypesStorage = storage.GetEventStore(eventStore).EventTypes;

        // Get the event type definition with all generations and migrations
        var definition = await eventTypesStorage.GetDefinition(eventType.Id);

        // If there's only one generation, reuse the already-built expando instead of re-converting.
        if (!definition.Generations.Skip(1).Any())
        {
            result[eventType.Generation] = contentAsExpandoObject;
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

        foreach (var migration in migrations.Where(migration => migration.FromGeneration == currentGeneration))
        {
            // Apply the upcast migration
            currentContent = ApplyUpcastMigration(currentContent, migration);
            currentGeneration = migration.ToGeneration;
            var targetGenerationDef = definition.Generations.First(g => g.Generation == currentGeneration);
            result[currentGeneration] = expandoObjectConverter.ToExpandoObject(currentContent, targetGenerationDef.Schema);
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

        foreach (var migration in migrations.Where(migration => migration.ToGeneration == currentGeneration))
        {
            // Apply the downcast migration (reverse direction)
            currentContent = ApplyDowncastMigration(currentContent, migration);
            currentGeneration = migration.FromGeneration;
            var targetGenerationDef = definition.Generations.First(g => g.Generation == currentGeneration);
            result[currentGeneration] = expandoObjectConverter.ToExpandoObject(currentContent, targetGenerationDef.Schema);
        }

        await Task.CompletedTask;
    }

    JsonObject ApplyUpcastMigration(JsonObject content, EventTypeMigrationDefinition migration)
    {
        return ApplyJmesPathTransformation(content, migration.UpcastJmesPath);
    }

    JsonObject ApplyDowncastMigration(JsonObject content, EventTypeMigrationDefinition migration)
    {
        return ApplyJmesPathTransformation(content, migration.DowncastJmesPath);
    }

    JsonObject ApplyJmesPathTransformation(JsonObject content, JsonObject? jmesPath)
    {
        // Apply JmesPath transformation if defined
        if (jmesPath?.Count == 0 || jmesPath is null)
        {
            return content;
        }

        // Separate built-in expressions from standard JmesPath expressions
        var defaultValues = new Dictionary<string, JsonNode?>();
        var customResults = new Dictionary<string, JsonNode?>();
        var regularJmesPath = new JsonObject();

        foreach (var property in jmesPath)
        {
            if (property.Value is JsonObject expr && expr.Count == 1)
            {
                using var enumerator = expr.GetEnumerator();
                enumerator.MoveNext();
                switch (enumerator.Current.Key)
                {
                    case WellKnownExpressions.DefaultValue:
                        defaultValues[property.Key] = expr[WellKnownExpressions.DefaultValue]?.DeepClone();
                        break;

                    case WellKnownExpressions.Split when expr[WellKnownExpressions.Split] is JsonObject splitConfig:
                        customResults[property.Key] = EventMigrationExpressions.EvaluateSplit(content, splitConfig);
                        break;

                    case WellKnownExpressions.Combine when expr[WellKnownExpressions.Combine] is JsonObject combineConfig:
                        customResults[property.Key] = EventMigrationExpressions.EvaluateCombine(content, combineConfig);
                        break;

                    case WellKnownExpressions.Rename:
                        customResults[property.Key] = EventMigrationExpressions.EvaluateRename(content, expr[WellKnownExpressions.Rename]?.GetValue<string>());
                        break;

                    case WellKnownExpressions.MapValues when expr[WellKnownExpressions.MapValues] is JsonObject mapConfig:
                        if (EventMigrationExpressions.TryEvaluateMapValues(content, mapConfig, out var mappedValue))
                        {
                            customResults[property.Key] = mappedValue;
                        }

                        break;

                    default:
                        regularJmesPath[property.Key] = property.Value.DeepClone();
                        break;
                }
            }
            else
            {
                regularJmesPath[property.Key] = property.Value?.DeepClone();
            }
        }

        // Compute base result: start from a copy of the original content so that
        // properties not explicitly mapped are preserved for schema-based filtering.
        JsonObject result;

        if (regularJmesPath.Count > 0)
        {
            try
            {
                var jmesPathExpr = regularJmesPath.ToJsonString();
                var contentJson = content.ToJsonString();
                using var contentDoc = JsonDocument.Parse(contentJson);
                using var resultDoc = JsonTransformer.Transform(contentDoc.RootElement, jmesPathExpr);
                var resultJson = JsonSerializer.Serialize(resultDoc.RootElement);
                result = JsonNode.Parse(resultJson)?.AsObject() ?? new JsonObject();
            }
            catch
            {
                // If transformation fails, preserve the original content
                result = JsonNode.Parse(content.ToJsonString())?.AsObject() ?? new JsonObject();
            }
        }
        else
        {
            // No standard JmesPath — start from a copy of the original content
            result = JsonNode.Parse(content.ToJsonString())?.AsObject() ?? new JsonObject();
        }

        // Apply custom expression results (split / combine / rename)
        foreach (var (propertyName, value) in customResults)
        {
            result[propertyName] = value?.DeepClone();
        }

        // Apply default values for any properties that are absent from the result
        foreach (var (propertyName, defaultValue) in defaultValues)
        {
            if (!result.ContainsKey(propertyName))
            {
                result[propertyName] = defaultValue?.DeepClone();
            }
        }

        return result;
    }
}


