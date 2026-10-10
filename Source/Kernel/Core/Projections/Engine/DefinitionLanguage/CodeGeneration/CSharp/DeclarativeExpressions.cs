// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Schemas;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.CSharp;

/// <summary>
/// Renders stored projection expressions as fluent C# operations.
/// </summary>
static class DeclarativeExpressions
{
    /// <summary>
    /// Renders the source of a set operation.
    /// </summary>
    /// <param name="expression">The stored source expression.</param>
    /// <returns>The fluent set continuation.</returns>
    internal static string Set(string expression)
    {
        var source = ProjectionExpressions.ReadValue(expression);
        return source.Kind switch
        {
            ProjectionValueKind.Nothing => "ToValue(null)",
            ProjectionValueKind.EventSourceId => "ToEventSourceId()",
            ProjectionValueKind.EventContextProperty => $"ToEventContextProperty(c => c.{CSharpDefinitionSupport.ContextPath(source.Value)})",
            ProjectionValueKind.Text => $"ToValue({SymbolDisplay.FormatLiteral(source.Value, true)})",
            ProjectionValueKind.Literal => $"ToValue({source.Value})",
            _ => $"To(e => e.{source.Value})"
        };
    }

    /// <summary>
    /// Renders a property mapping or arithmetic operation.
    /// </summary>
    /// <param name="property">The target property.</param>
    /// <param name="expression">The stored operation.</param>
    /// <returns>The fluent operation.</returns>
    internal static string Operation(string property, string expression)
    {
        var normalized = Normalize(expression);
        if (normalized.StartsWith($"{WellKnownExpressions.Add}(", StringComparison.Ordinal) && normalized.EndsWith(')'))
        {
            return $".Add(m => m.{property}).With(e => e.{normalized[(WellKnownExpressions.Add.Length + 1)..^1]})";
        }
        if (normalized.StartsWith($"{WellKnownExpressions.Subtract}(", StringComparison.Ordinal) && normalized.EndsWith(')'))
        {
            return $".Subtract(m => m.{property}).With(e => e.{normalized[(WellKnownExpressions.Subtract.Length + 1)..^1]})";
        }

        return normalized switch
        {
            WellKnownExpressions.Increment => $".Increment(m => m.{property})",
            WellKnownExpressions.Decrement => $".Decrement(m => m.{property})",
            WellKnownExpressions.Count => $".Count(m => m.{property})",
            WellKnownExpressions.Null => $".Clear(m => m.{property})",
            _ => $".Set(m => m.{property}).{Set(normalized)}"
        };
    }

    /// <summary>
    /// Renders an explicit event key.
    /// </summary>
    /// <param name="expression">The key expression.</param>
    /// <param name="schema">The model schema.</param>
    /// <returns>The fluent key configuration.</returns>
    internal static string Key(string expression, JsonSchema schema) => Key(expression, schema, false);

    /// <summary>
    /// Renders an explicit parent key.
    /// </summary>
    /// <param name="expression">The parent key expression.</param>
    /// <param name="schema">The model schema.</param>
    /// <returns>The fluent parent key configuration.</returns>
    internal static string ParentKey(string expression, JsonSchema schema) => Key(expression, schema, true);

    /// <summary>
    /// Normalizes operation aliases into stored expression names.
    /// </summary>
    /// <param name="expression">The operation expression.</param>
    /// <returns>The normalized expression.</returns>
    internal static string Normalize(string expression)
    {
        if (expression.StartsWith("+=", StringComparison.Ordinal)) return $"{WellKnownExpressions.Add}({expression[2..].Trim()})";
        if (expression.StartsWith("-=", StringComparison.Ordinal)) return $"{WellKnownExpressions.Subtract}({expression[2..].Trim()})";

        return expression switch
        {
            "increment" => WellKnownExpressions.Increment,
            "decrement" => WellKnownExpressions.Decrement,
            "count" => WellKnownExpressions.Count,
            _ => expression
        };
    }

    static string Key(string expression, JsonSchema schema, bool parent)
    {
        var prefix = parent ? "UsingParent" : "Using";
        if (expression.StartsWith($"{WellKnownExpressions.Composite}(", StringComparison.Ordinal))
        {
            var parsed = CompositeKeyExpression.Parse(expression);
            var typeName = parsed.TypeName ?? CompositeKeyTypeName.From(schema);
            var mappings = parsed.Mappings.Select(mapping => $".Set(k => k.{mapping.Property}).To({ValueLambda(mapping.Expression)})");
            return $".{prefix}CompositeKey<{typeName}>(_ => _{string.Concat(mappings)})";
        }

        var source = ProjectionExpressions.ReadValue(expression);
        return source.Kind switch
        {
            ProjectionValueKind.EventSourceId => $".{prefix}KeyFromContext(c => c.EventSourceId)",
            ProjectionValueKind.EventContextProperty => $".{prefix}KeyFromContext(c => c.{CSharpDefinitionSupport.ContextPath(source.Value)})",
            ProjectionValueKind.Text or ProjectionValueKind.Literal => parent
                ? $".UsingConstantParentKey({SymbolDisplay.FormatLiteral(source.Value, true)})"
                : $".UsingConstantKey({SymbolDisplay.FormatLiteral(source.Value, true)})",
            _ => $".{prefix}Key(e => e.{source.Value})"
        };
    }

    static string ValueLambda(string expression)
    {
        var source = ProjectionExpressions.ReadValue(expression);
        return source.Kind is ProjectionValueKind.EventContextProperty or ProjectionValueKind.EventSourceId
            ? $"c => c.{(source.Kind == ProjectionValueKind.EventSourceId ? "EventSourceId" : CSharpDefinitionSupport.ContextPath(source.Value))}"
            : $"e => e.{source.Value}";
    }
}
