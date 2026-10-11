// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.CSharp;

/// <summary>
/// Renders fluent projection relationship and removal blocks.
/// </summary>
static class DeclarativeRelationships
{
    /// <summary>
    /// Renders joins without specifying an On property in child scopes.
    /// </summary>
    /// <param name="joins">The joins to render.</param>
    /// <param name="lines">The generated fluent steps.</param>
    /// <param name="isChild">Whether these joins belong to children.</param>
    internal static void Joins(IDictionary<EventType, JoinDefinition> joins, List<string> lines, bool isChild)
    {
        foreach (var join in joins)
        {
            var operations = new List<string>();
            if (!isChild)
            {
                operations.Add($".On(m => m.{join.Value.On.Path})");
            }
            operations.AddRange(join.Value.Properties.Select(property => $".Set(m => m.{property.Key.Path}).{DeclarativeExpressions.Set(property.Value)}"));
            var callback = operations.Count == 0 ? string.Empty : $"j => j{string.Concat(operations)}";
            lines.Add($".Join<{join.Key.Id.Value}>({callback})");
        }
    }

    /// <summary>
    /// Renders child collections and nested objects recursively.
    /// </summary>
    /// <param name="children">The child scopes.</param>
    /// <param name="scope">The enclosing schema, output and renderer.</param>
    /// <param name="nested">Whether the scopes are single objects.</param>
    internal static void Children(IDictionary<PropertyPath, ChildrenDefinition> children, Scope scope, bool nested)
    {
        foreach (var child in children)
        {
            var childSchema = scope.Schema;
            if (scope.Schema.Properties.TryGetValue(child.Key.Path, out var property))
            {
                childSchema = nested ? property.ActualSchema : property.Item?.ActualSchema ?? property.ActualSchema;
            }
            var blocks = new List<string>();
            if (!nested)
            {
                blocks.Add($".IdentifiedBy(e => e.{child.Value.IdentifiedBy.Path})");
            }
            scope.Render(child.Value, childSchema, blocks, !nested);
            var builder = nested ? "nested" : "children";
            var method = nested ? "Nested" : "Children";
            scope.Lines.Add($".{method}(m => m.{child.Key.Path}, {builder} => {builder}{string.Concat(blocks)})");
        }
    }

    /// <summary>
    /// Renders removals through the client's key-builder callbacks.
    /// </summary>
    /// <param name="definition">The scope definition.</param>
    /// <param name="scope">The enclosing schema and output.</param>
    internal static void Removals(ProjectionDefinition definition, Scope scope)
    {
        foreach (var removed in definition.RemovedWith)
        {
            var operations = CSharpDefinitionSupport.IsDefaultKey(removed.Value.Key) ? string.Empty : DeclarativeExpressions.Key(removed.Value.Key, scope.Schema);
            if (!string.IsNullOrEmpty(removed.Value.ParentKey?.Value))
            {
                operations += DeclarativeExpressions.ParentKey(removed.Value.ParentKey, scope.Schema);
            }
            var callback = operations.Length == 0 ? string.Empty : $"_ => _{operations}";
            scope.Lines.Add($".RemovedWith<{removed.Key.Id.Value}>({callback})");
        }

        foreach (var removed in definition.RemovedWithJoin)
        {
            var callback = CSharpDefinitionSupport.IsDefaultKey(removed.Value.Key) ? string.Empty : $"_ => _{DeclarativeExpressions.Key(removed.Value.Key, scope.Schema)}";
            scope.Lines.Add($".RemovedWithJoin<{removed.Key.Id.Value}>({callback})");
        }
    }

    /// <summary>
    /// Carries the shared state for rendering a projection's relationships.
    /// </summary>
    /// <param name="Schema">The enclosing model schema.</param>
    /// <param name="Lines">The generated fluent steps.</param>
    /// <param name="Render">The recursive scope renderer.</param>
    internal sealed record Scope(JsonSchema Schema, List<string> Lines, Action<ProjectionDefinition, JsonSchema, List<string>, bool> Render);
}
