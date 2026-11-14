// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Validates an event type's migration chain and the properties its migrations address.
/// </summary>
static class EventTypeMigrationValidation
{
    /// <summary>
    /// Throws when the migration chain of an event type is broken.
    /// </summary>
    /// <param name="eventTypeId">The event type being validated.</param>
    /// <param name="currentGeneration">The current generation of the event type.</param>
    /// <param name="migrations">The migrations declared for the event type.</param>
    internal static void ValidateChain(string eventTypeId, uint currentGeneration, IList<Contracts.Events.EventTypeMigrationDefinition> migrations)
    {
        if (FindChainProblem(eventTypeId, currentGeneration, migrations) is { } problem)
        {
            throw problem;
        }
    }

    /// <summary>
    /// Finds the first problem in the migration chain of an event type.
    /// </summary>
    /// <param name="eventTypeId">The event type being validated.</param>
    /// <param name="currentGeneration">The current generation of the event type.</param>
    /// <param name="migrations">The migrations declared for the event type.</param>
    /// <returns>The exception describing the problem, or <see langword="null"/> when the chain is intact.</returns>
    internal static Exception? FindChainProblem(string eventTypeId, uint currentGeneration, IList<Contracts.Events.EventTypeMigrationDefinition> migrations)
    {
        if (currentGeneration <= 1)
        {
            return null;
        }

        var effectiveMigrations = migrations.Where(m => m.FromGeneration != m.ToGeneration).ToList();

        if (effectiveMigrations.Count == 0)
        {
            return new MissingEventTypeMigrators(eventTypeId, currentGeneration);
        }

        if (!effectiveMigrations.Exists(m => m.FromGeneration == 1))
        {
            return new MissingFirstGenerationForEventType(eventTypeId, currentGeneration);
        }

        return FindGap(effectiveMigrations, currentGeneration) is { } missing
            ? new MissingMigrationForEventTypeGeneration(eventTypeId, currentGeneration, missing)
            : null;
    }

    /// <summary>
    /// Throws when a migration addresses properties that its source or target generation schema does not declare.
    /// </summary>
    /// <param name="eventTypeId">The event type being validated.</param>
    /// <param name="migration">The migration to validate.</param>
    /// <param name="generations">The generations of the event type.</param>
    internal static void ValidateProperties(
        string eventTypeId,
        EventTypeMigrationDefinition migration,
        IEnumerable<EventTypeGenerationDefinition> generations)
    {
        var fromSchema = generations.FirstOrDefault(g => g.Generation == migration.FromGeneration)?.Schema;
        var toSchema = generations.FirstOrDefault(g => g.Generation == migration.ToGeneration)?.Schema;
        var upcast = new MigrationDirection(eventTypeId, migration.ToGeneration.Value, "upcast");
        var downcast = new MigrationDirection(eventTypeId, migration.FromGeneration.Value, "downcast");

        if (toSchema is not null)
        {
            ValidatePropertyKeys(upcast, migration.UpcastJmesPath, toSchema);
        }

        if (fromSchema is not null)
        {
            ValidatePropertyKeys(downcast, migration.DowncastJmesPath, fromSchema);
            ValidateExpressionSources(upcast with { Generation = migration.FromGeneration.Value }, migration.UpcastJmesPath, fromSchema);
        }

        if (toSchema is not null)
        {
            ValidateExpressionSources(downcast with { Generation = migration.ToGeneration.Value }, migration.DowncastJmesPath, toSchema);
        }
    }

    static uint? FindGap(List<Contracts.Events.EventTypeMigrationDefinition> migrations, uint currentGeneration)
    {
        for (uint from = 1; from < currentGeneration; from++)
        {
            if (!migrations.Exists(m => m.FromGeneration == from))
            {
                return from;
            }
        }

        return null;
    }

    static void ValidatePropertyKeys(MigrationDirection direction, JsonObject jmesPath, JsonSchema schema)
    {
        foreach (var property in jmesPath)
        {
            if (IsDefaultIntoEmptySchema(property.Value, schema))
            {
                continue;
            }

            if (!SchemaPaths.Declares(schema, property.Key))
            {
                throw new InvalidMigrationPropertyForEventType(direction.EventTypeId, property.Key, direction.Generation, direction.Name);
            }
        }
    }

    /// <summary>
    /// DefaultValue introduces a property to the target generation, and the auto-generated schema for that
    /// generation can be empty - so a key that resolves nowhere is only accepted when there is nothing to
    /// resolve it against. Skipping the check outright let a nested default through to a kernel that then
    /// dropped the value silently, which is worse than either outcome (#3949).
    /// </summary>
    /// <param name="expression">The expression a property key maps to.</param>
    /// <param name="schema">The <see cref="JsonSchema"/> the key is validated against.</param>
    /// <returns><see langword="true"/> when the key may be skipped; otherwise <see langword="false"/>.</returns>
    static bool IsDefaultIntoEmptySchema(JsonNode? expression, JsonSchema schema) =>
        expression is JsonObject expr &&
        expr.ContainsKey(WellKnownExpressions.DefaultValue) &&
        schema.ActualProperties.Count == 0;

    static void ValidateExpressionSources(MigrationDirection direction, JsonObject jmesPath, JsonSchema sourceSchema)
    {
        foreach (var source in jmesPath.SelectMany(entry => MigrationExpressionSources.From(entry.Value)))
        {
            if (!SchemaPaths.Declares(sourceSchema, source))
            {
                throw new InvalidMigrationPropertyForEventType(direction.EventTypeId, source, direction.Generation, direction.Name);
            }
        }
    }
}
