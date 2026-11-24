// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Expressions.ReadModelProperties;

/// <summary>
/// Reads one arithmetic operand or a single decimal product, preserving legacy single-operand coercion.
/// </summary>
internal static partial class ArithmeticOperand
{
    /// <summary>
    /// The bounded scalar or single-product expression pattern.
    /// </summary>
    internal const string Pattern = $"(?<left>{Scalar})(?: \\* (?<right>{Scalar}))?";
    const string Scalar = $"(?:{EventValueProviderRegularExpressions.Expression}|\\$eventContext\\([A-Za-z][A-Za-z0-9_]*(?:\\.[A-Za-z][A-Za-z0-9_]*(?:\\(\\))?)*\\))";

    [GeneratedRegex($"^{Pattern}$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex OperandPattern { get; }

    /// <summary>
    /// Resolves scalars with legacy coercion and products with decimal multiplication before target coercion.
    /// </summary>
    /// <param name="resolvers">The scalar value resolvers.</param>
    /// <param name="target">The target property schema.</param>
    /// <param name="expression">The scalar or product expression.</param>
    /// <returns>The value provider.</returns>
    internal static ValueProvider<AppendedEvent> Resolve(IEventValueProviderExpressionResolvers resolvers, JsonSchemaProperty target, string expression)
    {
        var match = OperandPattern.Match(expression);
        if (!match.Groups["right"].Success)
        {
            return resolvers.Resolve(target, expression);
        }

        // An untyped schema preserves the scalar value; do not coerce either operand to the target type.
        var scalar = new JsonSchemaProperty();
        var left = resolvers.Resolve(scalar, match.Groups["left"].Value);
        var right = resolvers.Resolve(scalar, match.Groups["right"].Value);
        return @event => ToDecimal(left(@event)) * ToDecimal(right(@event));
    }

    /// <summary>
    /// Formats the scalar operands without losing a product.
    /// </summary>
    /// <param name="expression">The scalar or product expression.</param>
    /// <param name="format">The scalar formatter.</param>
    /// <returns>The formatted expression.</returns>
    internal static string Format(string expression, Func<string, string> format)
    {
        var match = OperandPattern.Match(expression);
        return match.Groups["right"].Success
            ? $"{format(match.Groups["left"].Value)} * {format(match.Groups["right"].Value)}"
            : format(expression);
    }

    static decimal ToDecimal(object? value)
    {
        var primitive = value?.IsConcept() == true ? value.GetConceptValue() : value;
        return primitive switch
        {
            sbyte or byte or short or ushort or int or uint or long or ulong or decimal => Convert.ToDecimal(primitive, CultureInfo.InvariantCulture),
            float or double => DecimalValues.ToDecimal(primitive),
            _ => throw new InvalidOperationException("Arithmetic products require non-null numeric operands")
        };
    }
}
