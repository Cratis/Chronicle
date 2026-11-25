// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.Engine.Expressions.Keys;

/// <summary>
/// A flat (untyped) composite key expression: two or more root read model properties, each with the expression providing its value.
/// </summary>
/// <param name="Parts">The key parts, ordered by part name using an ordinal comparison.</param>
public record FlatCompositeKeyExpression(IReadOnlyList<(string Property, string Expression)> Parts)
{
    /// <summary>
    /// The smallest number of parts a flat composite key has.
    /// </summary>
    public const int MinimumParts = 2;

    /// <summary>
    /// Gets whether an expression is a flat composite key expression.
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <returns>True if it starts as a flat composite key expression, false if not.</returns>
    public static bool IsFlatComposite(string expression) => expression.StartsWith($"{WellKnownExpressions.FlatComposite}(", StringComparison.Ordinal);

    /// <summary>
    /// Creates the expression text for a set of parts, ordering them by part name.
    /// </summary>
    /// <param name="parts">The key parts.</param>
    /// <returns>The expression text.</returns>
    public static string Format(IEnumerable<(string Property, string Expression)> parts) =>
        $"{WellKnownExpressions.FlatComposite}({string.Join(", ", parts.OrderBy(_ => _.Property, StringComparer.Ordinal).Select(_ => $"{_.Property}={_.Expression}"))})";

    /// <summary>
    /// Parses a flat composite key expression.
    /// </summary>
    /// <param name="expression">The complete expression.</param>
    /// <returns>The parsed key, with its parts ordered by part name.</returns>
    /// <exception cref="InvalidFlatCompositeKeyExpression">The expression is malformed.</exception>
    public static FlatCompositeKeyExpression Parse(string expression)
    {
        var prefix = $"{WellKnownExpressions.FlatComposite}(";
        if (!expression.StartsWith(prefix, StringComparison.Ordinal) || !expression.EndsWith(')'))
        {
            throw new InvalidFlatCompositeKeyExpression(expression, $"expected {prefix}part=expression, part=expression)");
        }

        var parts = new List<(string Property, string Expression)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var component in SplitTopLevel(expression, expression[prefix.Length..^1]))
        {
            var separator = component.IndexOf('=');
            if (separator <= 0 || separator == component.Length - 1)
            {
                throw new InvalidFlatCompositeKeyExpression(expression, $"component '{component}' must be property=expression");
            }

            var property = component[..separator].Trim();
            var value = component[(separator + 1)..].Trim();
            ThrowIfInvalidPartName(expression, property);
            if (value.Length == 0)
            {
                throw new InvalidFlatCompositeKeyExpression(expression, $"part '{property}' has no expression");
            }

            if (!seen.Add(property))
            {
                throw new InvalidFlatCompositeKeyExpression(expression, $"part '{property}' is given more than once");
            }

            parts.Add((property, value));
        }

        if (parts.Count < MinimumParts)
        {
            throw new InvalidFlatCompositeKeyExpression(expression, $"a flat composite key needs at least {MinimumParts} parts");
        }

        parts.Sort((left, right) => StringComparer.Ordinal.Compare(left.Property, right.Property));
        return new FlatCompositeKeyExpression(parts);
    }

    static void ThrowIfInvalidPartName(string expression, string property)
    {
        if (property.Length == 0 || !(char.IsLetter(property[0]) || property[0] == '_') || !property.All(_ => char.IsLetterOrDigit(_) || _ == '_'))
        {
            throw new InvalidFlatCompositeKeyExpression(expression, $"part '{property}' must be a simple root property name");
        }

        if (property[0] == '_')
        {
            throw new InvalidFlatCompositeKeyExpression(expression, $"part '{property}' uses a reserved name; names starting with '_' are used by the sink");
        }

        if (property.Equals("id", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidFlatCompositeKeyExpression(expression, $"part '{property}' is not supported as a flat composite key part; use a more specific name");
        }
    }

    static List<string> SplitTopLevel(string expression, string content)
    {
        var components = new List<string>();
        var start = 0;
        var depth = 0;
        char? quote = null;
        for (var index = 0; index < content.Length; index++)
        {
            var current = content[index];
            if (quote is not null)
            {
                if (current == '\\') index++;
                else if (current == quote) quote = null;
                continue;
            }

            switch (current)
            {
                case '"' or '\'':
                    quote = current;
                    break;
                case '(' or '[':
                    depth++;
                    break;
                case ')' or ']':
                    depth--;
                    break;
                case ',' when depth == 0:
                    components.Add(content[start..index].Trim());
                    start = index + 1;
                    break;
            }
        }

        if (quote is not null || depth != 0)
        {
            throw new InvalidFlatCompositeKeyExpression(expression, "unbalanced quotes or brackets");
        }

        components.Add(content[start..].Trim());
        return components;
    }
}
