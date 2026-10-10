// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.CSharp;

/// <summary>
/// Resolves stored context expressions and schema types to their C# spelling.
/// </summary>
static class CSharpDefinitionSupport
{
    /// <summary>
    /// Converts context property paths to C# member casing.
    /// </summary>
    /// <param name="path">The stored context path.</param>
    /// <returns>The C# property path.</returns>
    internal static string ContextPath(string path) => string.Join('.', path.Split('.').Select(part => char.ToUpperInvariant(part[0]) + part[1..]));

    /// <summary>
    /// Determines whether a key uses the default event source identifier.
    /// </summary>
    /// <param name="expression">The stored key expression.</param>
    /// <returns>Whether explicit key configuration can be omitted.</returns>
    internal static bool IsDefaultKey(string expression) => string.IsNullOrEmpty(expression) || expression == WellKnownExpressions.EventSourceId;

    /// <summary>
    /// Converts stored key expressions to their attribute spelling.
    /// </summary>
    /// <param name="expression">The key expression.</param>
    /// <returns>The attribute key value.</returns>
    internal static string AttributeKey(string expression)
    {
        var value = ProjectionExpressions.ReadValue(expression);
        if (value.Kind == ProjectionValueKind.EventContextProperty)
        {
            return value.Value.Equals("eventSourceId", StringComparison.OrdinalIgnoreCase)
                ? WellKnownExpressions.EventSourceId
                : $"{WellKnownExpressions.EventContext}({ContextPath(value.Value)})";
        }

        return expression;
    }

    /// <summary>
    /// Resolves schema formats and collection elements to C# types.
    /// </summary>
    /// <param name="schema">The property schema.</param>
    /// <param name="fallbackName">The record name for an unnamed object.</param>
    /// <returns>The C# type name.</returns>
    internal static string ModelType(JsonSchema schema, string fallbackName)
    {
        var actual = schema.ActualSchema;
        return actual.Type switch
        {
            JsonObjectType.String => actual.Format switch
            {
                "date-time" => "System.DateTimeOffset",
                "uuid" or "guid" => "System.Guid",
                _ => "string"
            },
            JsonObjectType.Integer => "int",
            JsonObjectType.Number => "decimal",
            JsonObjectType.Boolean => "bool",
            JsonObjectType.Array when actual.Item is not null => $"System.Collections.Generic.IEnumerable<{ModelType(actual.Item, fallbackName)}>",
            JsonObjectType.Object => RecordName(actual, fallbackName),
            _ => "object"
        };
    }

    /// <summary>
    /// Resolves the name of an object record.
    /// </summary>
    /// <param name="schema">The object schema.</param>
    /// <param name="fallbackName">The name for an unnamed schema.</param>
    /// <returns>The record name.</returns>
    internal static string RecordName(JsonSchema schema, string fallbackName) => schema.Title ?? fallbackName;

    /// <summary>
    /// Gets the projection scope for a child or nested object.
    /// </summary>
    /// <param name="definition">The enclosing definition.</param>
    /// <param name="property">The child property.</param>
    /// <returns>The child definition or an empty scope.</returns>
    internal static ChildrenDefinition ChildDefinition(ProjectionDefinition definition, string property)
    {
        if (definition.Children.TryGetValue(new PropertyPath(property), out var child) || definition.Nested?.TryGetValue(new PropertyPath(property), out child) == true)
        {
            return child;
        }

        return new ChildrenDefinition(
            PropertyPath.NotSet,
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), true),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>());
    }
}
