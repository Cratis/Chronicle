// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Chronicle.Projections.Engine.Expressions.Keys;

/// <summary>
/// A composite key expression with an optional type name and one or more property mappings.
/// </summary>
/// <param name="TypeName">The optional key type name.</param>
/// <param name="Mappings">The key property and value expression pairs.</param>
public record CompositeKeyExpression(string? TypeName, IReadOnlyList<(string Property, string Expression)> Mappings)
{
    /// <summary>
    /// Parses a composite key expression, accepting both typed and legacy untyped forms.
    /// </summary>
    /// <param name="expression">The complete expression.</param>
    /// <returns>The parsed composite key.</returns>
    /// <exception cref="InvalidCompositeKeyExpression">The expression is malformed.</exception>
    public static CompositeKeyExpression Parse(string expression)
    {
        var prefix = $"{WellKnownExpressions.Composite}(";
        if (!expression.StartsWith(prefix, StringComparison.Ordinal) || !expression.EndsWith(')'))
        {
            throw new InvalidCompositeKeyExpression(expression, expression);
        }

        var content = expression[prefix.Length..^1];
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidCompositeKeyExpression(expression, string.Empty);
        }

        // Generic type arguments may contain commas; only top-level commas separate mappings.
        var components = new List<string>();
        var startOfComponent = 0;
        var genericDepth = 0;
        for (var index = 0; index < content.Length; index++)
        {
            if (content[index] == '<') genericDepth++;
            if (content[index] == '>') genericDepth--;
            if (content[index] == ',' && genericDepth == 0)
            {
                components.Add(content[startOfComponent..index].Trim());
                startOfComponent = index + 1;
            }
        }
        components.Add(content[startOfComponent..].Trim());
        string? typeName = null;
        var start = 0;
        if (!components[0].Contains('='))
        {
            typeName = components[0];
            var parsedType = SyntaxFactory.ParseTypeName(typeName);
            if (parsedType is not IdentifierNameSyntax and not GenericNameSyntax and not QualifiedNameSyntax ||
                parsedType.ContainsDiagnostics || parsedType.ToFullString() != typeName)
            {
                throw new InvalidCompositeKeyExpression(expression, typeName);
            }
            start = 1;
        }

        if ((string.IsNullOrWhiteSpace(typeName) && start == 1) || components.Count == start)
        {
            throw new InvalidCompositeKeyExpression(expression, components[0]);
        }

        var mappings = new List<(string Property, string Expression)>();
        var seenProperties = new HashSet<string>(StringComparer.Ordinal);
        foreach (var component in components.Skip(start))
        {
            var separator = component.IndexOf('=');
            if (separator <= 0 || separator == component.Length - 1 || component.IndexOf('=', separator + 1) >= 0)
            {
                throw new InvalidCompositeKeyExpression(expression, component);
            }

            var property = component[..separator].Trim();
            var value = component[(separator + 1)..].Trim();
            if (property.Length == 0 || value.Length == 0 || !seenProperties.Add(property))
            {
                throw new InvalidCompositeKeyExpression(expression, component);
            }
            mappings.Add((property, value));
        }

        return new CompositeKeyExpression(typeName, mappings);
    }
}
