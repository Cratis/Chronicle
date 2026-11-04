// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Services.Projections;

/// <summary>
/// Checks whether an inferred read model schema covers a projection's static targets.
/// </summary>
internal static class InferredReadModelCoverage
{
    /// <summary>
    /// Finds the targets that the inferred schema cannot hold, in declaration walk order.
    /// </summary>
    /// <param name="definition">The compiled projection definition.</param>
    /// <param name="schema">The inferred read model schema.</param>
    /// <returns>Distinct, readable labels for the uncovered targets.</returns>
    public static IReadOnlyList<string> FindUncoveredTargets(ProjectionDefinition definition, JsonSchema schema)
    {
        var uncovered = new List<string>();
        Walk(definition, new Scope(schema, string.Empty), uncovered);
        return uncovered;
    }

    static void Walk(ProjectionDefinition level, Scope scope, List<string> uncovered)
    {
        WalkProperties(level, scope, uncovered);
        WalkNested(level, scope, uncovered);
        WalkChildren(level, scope, uncovered);
    }

    static void WalkProperties(ProjectionDefinition level, Scope scope, List<string> uncovered)
    {
        foreach (var target in level.FromEvery.Properties.Keys)
        {
            Check(target, " (every)", scope, uncovered);
        }
        foreach (var (target, expression) in level.From.Values.SelectMany(from => from.Properties))
        {
            Check(target, IsCounter(expression) ? " (counter)" : string.Empty, scope, uncovered);
        }
        foreach (var target in level.Join.Values.SelectMany(join => join.Properties.Keys))
        {
            Check(target, " (join)", scope, uncovered);
        }
    }

    static void WalkNested(ProjectionDefinition level, Scope scope, List<string> uncovered)
    {
        if (level.Nested is null)
        {
            return;
        }
        foreach (var (target, nested) in level.Nested)
        {
            if (!IsStatic(target))
            {
                continue;
            }
            var property = Resolve(scope.Schema, target);
            if (property is null)
            {
                Add(uncovered, $"{scope.Prefix}{target.Path} (nested object)");
            }
            Walk(nested, scope.Into(property?.ActualTypeSchema, target), uncovered);
        }
    }

    static void WalkChildren(ProjectionDefinition level, Scope scope, List<string> uncovered)
    {
        foreach (var (target, child) in level.Children)
        {
            if (!IsStatic(target))
            {
                continue;
            }
            var property = Resolve(scope.Schema, target);
            var item = property is { IsArray: true, Item: not null } ? property.Item.ActualSchema : null;
            if (item is null)
            {
                Add(uncovered, $"{scope.Prefix}{target.Path} (child collection)");
            }
            Walk(child, scope.Into(item, target), uncovered);
        }
    }

    static void Check(PropertyPath target, string label, Scope scope, List<string> uncovered)
    {
        if (IsStatic(target) && Resolve(scope.Schema, target) is null)
        {
            Add(uncovered, $"{scope.Prefix}{target.Path}{label}");
        }
    }

    static bool IsStatic(PropertyPath path) => path.IsSet && path.LastSegment is not ThisAccessor && !path.Path.Contains('$');

    static JsonSchemaProperty? Resolve(JsonSchema? schema, PropertyPath path) =>
        schema?.GetSchemaPropertyForPropertyPath(path) ?? schema?.GetSchemaPropertyForPropertyPath(new PropertyPath(path.LastSegment.Value));

    static bool IsCounter(string expression) =>
        expression.StartsWith(WellKnownExpressions.Increment, StringComparison.Ordinal) ||
        expression.StartsWith(WellKnownExpressions.Decrement, StringComparison.Ordinal) ||
        expression.StartsWith(WellKnownExpressions.Count, StringComparison.Ordinal) ||
        expression.StartsWith(WellKnownExpressions.Add, StringComparison.Ordinal) ||
        expression.StartsWith(WellKnownExpressions.Subtract, StringComparison.Ordinal);

    static void Add(List<string> uncovered, string label)
    {
        if (!uncovered.Contains(label))
        {
            uncovered.Add(label);
        }
    }

    sealed record Scope(JsonSchema? Schema, string Prefix)
    {
        public Scope Into(JsonSchema? schema, PropertyPath target) => new(schema, $"{Prefix}{target.Path}.");
    }
}
