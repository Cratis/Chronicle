// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.CSharp;

/// <summary>
/// Renders stored projection expressions as fluent C# operations.
/// </summary>
static class DeclarativeExpressions
{
    enum ArithmeticKind
    {
        Add = 0,
        Subtract = 1
    }

    /// <summary>
    /// Renders the source of a set operation.
    /// </summary>
    /// <param name="expression">The stored source expression.</param>
    /// <returns>The fluent set continuation.</returns>
    internal static string Set(PropertyExpression expression)
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
    internal static string Operation(PropertyPath property, PropertyExpression expression)
    {
        var normalized = Normalize(expression);
        var arithmetic = ArithmeticFor(normalized);
        return arithmetic is null
            ? SimpleOperation(property, normalized)
            : $".{arithmetic.Kind}(m => m.{property.Path}).With(e => e.{arithmetic.Operand.Value})";
    }

    /// <summary>
    /// Renders an explicit event key.
    /// </summary>
    /// <param name="expression">The key expression.</param>
    /// <param name="schema">The model schema.</param>
    /// <returns>The fluent key configuration.</returns>
    internal static string Key(PropertyExpression expression, JsonSchema schema) => Key(expression, schema, false);

    /// <summary>
    /// Renders an explicit parent key.
    /// </summary>
    /// <param name="expression">The parent key expression.</param>
    /// <param name="schema">The model schema.</param>
    /// <returns>The fluent parent key configuration.</returns>
    internal static string ParentKey(PropertyExpression expression, JsonSchema schema) => Key(expression, schema, true);

    /// <summary>
    /// Normalizes operation aliases into stored expression names.
    /// </summary>
    /// <param name="expression">The operation expression.</param>
    /// <returns>The normalized expression.</returns>
    internal static string Normalize(PropertyExpression expression)
    {
        var value = expression.Value;
        if (value.StartsWith("+=", StringComparison.Ordinal)) return $"{WellKnownExpressions.Add}({value[2..].Trim()})";
        if (value.StartsWith("-=", StringComparison.Ordinal)) return $"{WellKnownExpressions.Subtract}({value[2..].Trim()})";

        return value switch
        {
            "increment" => WellKnownExpressions.Increment,
            "decrement" => WellKnownExpressions.Decrement,
            "count" => WellKnownExpressions.Count,
            _ => value
        };
    }

    static Arithmetic? ArithmeticFor(PropertyExpression expression) => expression.Value switch
    {
        var value when CallArgument(value, WellKnownExpressions.Add) is { } operand => new(ArithmeticKind.Add, operand),
        var value when CallArgument(value, WellKnownExpressions.Subtract) is { } operand => new(ArithmeticKind.Subtract, operand),
        _ => null
    };

    static string? CallArgument(PropertyExpression expression, PropertyExpression operation)
    {
        var prefix = $"{operation.Value}(";
        return expression.Value.StartsWith(prefix, StringComparison.Ordinal) && expression.Value.EndsWith(')')
            ? expression.Value[prefix.Length..^1]
            : null;
    }

    static string SimpleOperation(PropertyPath property, PropertyExpression expression) => expression.Value switch
    {
        WellKnownExpressions.Increment => $".Increment(m => m.{property.Path})",
        WellKnownExpressions.Decrement => $".Decrement(m => m.{property.Path})",
        WellKnownExpressions.Count => $".Count(m => m.{property.Path})",
        WellKnownExpressions.Null => $".Clear(m => m.{property.Path})",
        _ => $".Set(m => m.{property.Path}).{Set(expression)}"
    };

    static string Key(PropertyExpression expression, JsonSchema schema, bool parent)
    {
        var prefix = parent ? "UsingParent" : "Using";
        return expression.Value.StartsWith($"{WellKnownExpressions.Composite}(", StringComparison.Ordinal)
            ? CompositeKey(expression, schema, prefix)
            : ValueKey(ProjectionExpressions.ReadValue(expression), prefix);
    }

    static string CompositeKey(PropertyExpression expression, JsonSchema schema, string prefix)
    {
        var parsed = CompositeKeyExpression.Parse(expression);
        var typeName = parsed.TypeName ?? CompositeKeyTypeName.From(schema);
        var mappings = parsed.Mappings.Select(mapping => $".Set(k => k.{mapping.Property}).To({ValueLambda(mapping.Expression)})");
        return $".{prefix}CompositeKey<{typeName}>(_ => _{string.Concat(mappings)})";
    }

    static string ValueKey(ProjectionValueSource source, string prefix) => source.Kind switch
    {
        ProjectionValueKind.EventSourceId => $".{prefix}KeyFromContext(c => c.EventSourceId)",
        ProjectionValueKind.EventContextProperty => $".{prefix}KeyFromContext(c => c.{CSharpDefinitionSupport.ContextPath(source.Value)})",
        ProjectionValueKind.Text or ProjectionValueKind.Literal => ConstantKey(source, prefix),
        _ => $".{prefix}Key(e => e.{source.Value})"
    };

    static string ConstantKey(ProjectionValueSource source, string prefix)
    {
        var method = prefix == "UsingParent" ? "UsingConstantParentKey" : "UsingConstantKey";
        return $".{method}({SymbolDisplay.FormatLiteral(source.Value, true)})";
    }

    static string ValueLambda(PropertyExpression expression)
    {
        var source = ProjectionExpressions.ReadValue(expression);
        return source.Kind is ProjectionValueKind.EventContextProperty or ProjectionValueKind.EventSourceId
            ? $"c => c.{(source.Kind == ProjectionValueKind.EventSourceId ? "EventSourceId" : CSharpDefinitionSupport.ContextPath(source.Value))}"
            : $"e => e.{source.Value}";
    }

    sealed record Arithmetic(ArithmeticKind Kind, PropertyExpression Operand);
}
