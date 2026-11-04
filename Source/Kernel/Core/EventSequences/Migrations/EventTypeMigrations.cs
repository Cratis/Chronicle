// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
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
    public async Task<IDictionary<EventTypeGeneration, ExpandoObject>> MigrateToAllGenerations(EventStoreName eventStore, EventType eventType, JsonObject content, ExpandoObject contentAsExpandoObject, Action<JsonObject, JsonSchema, ExpandoObject>? onConverted = null, Action<JsonObject, JsonObject>? onMigrating = null)
    {
        var definition = await storage.GetEventStore(eventStore).EventTypes.GetDefinition(eventType.Id);

        return await MigrateToAllGenerations(definition, eventType, content, contentAsExpandoObject, onConverted, onMigrating);
    }

    /// <inheritdoc/>
    public async Task<IDictionary<EventTypeGeneration, ExpandoObject>> MigrateToAllGenerations(EventTypeDefinition definition, EventType eventType, JsonObject content, ExpandoObject contentAsExpandoObject, Action<JsonObject, JsonSchema, ExpandoObject>? onConverted = null, Action<JsonObject, JsonObject>? onMigrating = null)
    {
        var result = new Dictionary<EventTypeGeneration, ExpandoObject>();

        // If there's only one generation, reuse the already-built expando instead of re-converting.
        if (!definition.Generations.Skip(1).Any())
        {
            onConverted?.Invoke(content, definition.Generations.Single(g => g.Generation == eventType.Generation).Schema, contentAsExpandoObject);
            result[eventType.Generation] = contentAsExpandoObject;
            return result;
        }

        // Add the source generation
        var sourceGenerationDef = definition.Generations.First(g => g.Generation == eventType.Generation);
        result[eventType.Generation] = Convert(content, sourceGenerationDef.Schema, onConverted);

        // Upcast to higher generations
        await UpcastToHigherGenerations(eventType.Generation, content, definition, result, onConverted, onMigrating);

        // Downcast to lower generations
        await DowncastToLowerGenerations(eventType.Generation, content, definition, result, onConverted, onMigrating);

        return result;
    }

    static void ApplyToElements(JsonObject result, JsonObject content, string target, string kind, JsonNode? configuration)
    {
        var marker = target.IndexOf(JsonPropertyPaths.ElementMarker, StringComparison.Ordinal);
        var collectionPath = target[..marker];
        var remainder = target[(marker + JsonPropertyPaths.ElementMarker.Length)..].TrimStart('.');
        var elementPrefix = $"{collectionPath}{JsonPropertyPaths.ElementMarker}.";

        if (!JsonPropertyPaths.TryResolve(result, collectionPath, out var resultNode) || resultNode is not JsonArray resultElements ||
            !JsonPropertyPaths.TryResolve(content, collectionPath, out var contentNode) || contentNode is not JsonArray contentElements ||
            resultElements.Count != contentElements.Count)
        {
            return;
        }

        // Sources are written the way the target is, with the collection in them, and are read from the element.
        var relative = RelativeTo(configuration, kind, elementPrefix);

        for (var index = 0; index < resultElements.Count; index++)
        {
            if (resultElements[index] is not JsonObject resultElement || contentElements[index] is not JsonObject contentElement)
            {
                continue;
            }

            if (JsonPropertyPaths.AddressesElements(remainder))
            {
                ApplyToElements(resultElement, contentElement, remainder, kind, relative);
                continue;
            }

            switch (kind)
            {
                case WellKnownExpressions.DefaultValue:
                    JsonPropertyPaths.SetWhereAbsent(resultElement, remainder, relative?.DeepClone());
                    break;

                case WellKnownExpressions.Split when relative is JsonObject split:
                    JsonPropertyPaths.Set(resultElement, remainder, EventMigrationExpressions.EvaluateSplit(contentElement, split));
                    break;

                case WellKnownExpressions.Combine when relative is JsonObject combine:
                    JsonPropertyPaths.Set(resultElement, remainder, EventMigrationExpressions.EvaluateCombine(contentElement, combine));
                    break;

                case WellKnownExpressions.Rename:
                    JsonPropertyPaths.Set(resultElement, remainder, EventMigrationExpressions.EvaluateRename(contentElement, relative?.GetValue<string>())?.DeepClone());
                    break;

                case WellKnownExpressions.MapValues when relative is JsonObject map &&
                    EventMigrationExpressions.TryEvaluateMapValues(contentElement, map, out var mapped):
                    JsonPropertyPaths.Set(resultElement, remainder, mapped?.DeepClone());
                    break;
            }
        }
    }

    static JsonNode? RelativeTo(JsonNode? configuration, string kind, string elementPrefix)
    {
        string Strip(string path) => path.StartsWith(elementPrefix, StringComparison.Ordinal) ? path[elementPrefix.Length..] : path;

        switch (kind)
        {
            case WellKnownExpressions.Rename when configuration is JsonValue renamed && renamed.TryGetValue<string>(out var oldName):
                return JsonValue.Create(Strip(oldName));

            case WellKnownExpressions.Split or WellKnownExpressions.MapValues when configuration is JsonObject single:
                var copy = (JsonObject)single.DeepClone();
                if (copy["source"]?.GetValue<string>() is { } source)
                {
                    copy["source"] = Strip(source);
                }

                return copy;

            case WellKnownExpressions.Combine when configuration is JsonObject combine:
                var combined = (JsonObject)combine.DeepClone();
                if (combined["sources"] is JsonArray sources)
                {
                    combined["sources"] = new JsonArray([.. sources.Select(_ => (JsonNode?)JsonValue.Create(Strip(_!.GetValue<string>())))]);
                }

                return combined;

            default:
                return configuration?.DeepClone();
        }
    }

    ExpandoObject Convert(JsonObject content, JsonSchema schema, Action<JsonObject, JsonSchema, ExpandoObject>? onConverted)
    {
        var converted = expandoObjectConverter.ToExpandoObject(content, schema);
        onConverted?.Invoke(content, schema, converted);
        return converted;
    }

    async Task UpcastToHigherGenerations(
        EventTypeGeneration sourceGeneration,
        JsonObject sourceContent,
        EventTypeDefinition definition,
        Dictionary<EventTypeGeneration, ExpandoObject> result,
        Action<JsonObject, JsonSchema, ExpandoObject>? onConverted,
        Action<JsonObject, JsonObject>? onMigrating)
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
            onMigrating?.Invoke(migration.UpcastJmesPath, currentContent);
            currentContent = ApplyUpcastMigration(currentContent, migration);
            currentGeneration = migration.ToGeneration;
            var targetGenerationDef = definition.Generations.First(g => g.Generation == currentGeneration);
            var converted = Convert(currentContent, targetGenerationDef.Schema, onConverted);
            result[currentGeneration] = converted;
            currentContent = MigrationContentProjection.Project(currentContent, targetGenerationDef.Schema, converted);
        }

        await Task.CompletedTask;
    }

    async Task DowncastToLowerGenerations(
        EventTypeGeneration sourceGeneration,
        JsonObject sourceContent,
        EventTypeDefinition definition,
        Dictionary<EventTypeGeneration, ExpandoObject> result,
        Action<JsonObject, JsonSchema, ExpandoObject>? onConverted,
        Action<JsonObject, JsonObject>? onMigrating)
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
            onMigrating?.Invoke(migration.DowncastJmesPath, currentContent);
            currentContent = ApplyDowncastMigration(currentContent, migration);
            currentGeneration = migration.FromGeneration;
            var targetGenerationDef = definition.Generations.First(g => g.Generation == currentGeneration);
            var converted = Convert(currentContent, targetGenerationDef.Schema, onConverted);
            result[currentGeneration] = converted;
            currentContent = MigrationContentProjection.Project(currentContent, targetGenerationDef.Schema, converted);
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
        var elementOperations = new List<(string Target, string Kind, JsonNode? Configuration)>();
        var regularJmesPath = new JsonObject();

        foreach (var property in jmesPath)
        {
            if (JsonPropertyPaths.AddressesElements(property.Key) &&
                property.Value is JsonObject elementExpression && elementExpression.Count == 1)
            {
                // A target inside the elements of a collection is applied element by element once the result exists.
                var (kind, configuration) = elementExpression.Single();
                elementOperations.Add((property.Key, kind, configuration?.DeepClone()));
            }
            else if (property.Value is JsonObject expr && expr.Count == 1)
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

        // Apply custom expression results (split / combine / rename). The property name is a path, so a target
        // inside a nested object is written where it belongs instead of becoming a top-level key with a dot in
        // its name that the target generation's schema then discards (#3949).
        foreach (var (propertyName, value) in customResults)
        {
            JsonPropertyPaths.Set(result, propertyName, value?.DeepClone());
        }

        // Apply default values for any properties that are absent from the result
        foreach (var (propertyName, defaultValue) in defaultValues)
        {
            // Present-but-null still counts as present, exactly as the flat containment check it replaces did -
            // a default fills in a property the payload does not carry, not one that carries no value.
            if (!JsonPropertyPaths.TryResolve(result, propertyName, out _))
            {
                JsonPropertyPaths.Set(result, propertyName, defaultValue?.DeepClone());
            }
        }

        foreach (var (target, kind, configuration) in elementOperations)
        {
            ApplyToElements(result, content, target, kind, configuration);
        }

        return result;
    }
}
