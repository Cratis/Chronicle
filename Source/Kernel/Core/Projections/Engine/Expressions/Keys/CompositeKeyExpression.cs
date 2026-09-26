// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Expressions.Keys;

/// <summary>
/// A composite key's optional type name and its property-to-expression mappings.
/// </summary>
/// <param name="TypeName">The declared type, if present.</param>
/// <param name="Mappings">The component mappings.</param>
internal sealed record CompositeKeyExpression(string? TypeName, IReadOnlyList<KeyValuePair<string, string>> Mappings)
{
    /// <summary>
    /// Parses typed and untyped composite key expressions.
    /// </summary>
    /// <param name="expression">The stored expression.</param>
    /// <param name="projectionId">The projection owning the key.</param>
    /// <param name="identifiedByProperty">The path identifying the key.</param>
    /// <returns>The parsed expression.</returns>
    /// <exception cref="MissingCompositeExpressions">Thrown for empty composite keys.</exception>
    /// <exception cref="InvalidCompositeKeyPropertyMappingExpression">Thrown for malformed mappings.</exception>
    internal static CompositeKeyExpression Parse(string expression, ProjectionId projectionId, PropertyPath identifiedByProperty)
    {
        var prefix = $"{WellKnownExpressions.Composite}(";
        if (!expression.StartsWith(prefix, StringComparison.Ordinal) || !expression.EndsWith(')'))
        {
            throw new InvalidCompositeKeyPropertyMappingExpression(projectionId, identifiedByProperty, expression);
        }

        var content = expression[prefix.Length..^1];
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new MissingCompositeExpressions(projectionId, identifiedByProperty, expression);
        }

        var parts = content.Split(',').Select(_ => _.Trim()).ToArray();
        string? typeName = null;
        var startIndex = 0;
        if (!parts[0].Contains('='))
        {
            typeName = parts[0];
            if (typeName.Length == 0)
            {
                throw new InvalidCompositeKeyPropertyMappingExpression(projectionId, identifiedByProperty, parts[0]);
            }
            startIndex = 1;
        }

        if (startIndex == parts.Length)
        {
            throw new InvalidCompositeKeyPropertyMappingExpression(projectionId, identifiedByProperty, parts[0]);
        }

        var mappings = new List<KeyValuePair<string, string>>();
        foreach (var part in parts.Skip(startIndex))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0 || separator == part.Length - 1 || part.IndexOf('=', separator + 1) >= 0)
            {
                throw new InvalidCompositeKeyPropertyMappingExpression(projectionId, identifiedByProperty, part);
            }

            var property = part[..separator].Trim();
            var value = part[(separator + 1)..].Trim();
            if (property.Length == 0 || value.Length == 0)
            {
                throw new InvalidCompositeKeyPropertyMappingExpression(projectionId, identifiedByProperty, part);
            }

            mappings.Add(new(property, value));
        }

        return new CompositeKeyExpression(typeName, mappings);
    }

    /// <summary>
    /// Finds the key type in the read-model schema when it was omitted from the stored expression.
    /// </summary>
    /// <param name="schema">The read-model schema.</param>
    /// <returns>The declared, inferred, or fallback type name.</returns>
    internal string TypeNameOrFrom(JsonSchema schema)
    {
        if (!string.IsNullOrWhiteSpace(TypeName))
        {
            return TypeName;
        }

        var mappedProperties = Mappings.Select(_ => _.Key).ToArray();
        foreach (var (name, definition) in schema.Definitions)
        {
            if (mappedProperties.All(definition.Properties.ContainsKey))
            {
                return definition.Title ?? name;
            }
        }

        foreach (var property in schema.Properties.Values)
        {
            var actual = property.ActualSchema;
            if (!string.IsNullOrWhiteSpace(actual.Title) && mappedProperties.All(actual.Properties.ContainsKey))
            {
                return actual.Title;
            }
        }

        return "CompositeKey";
    }
}
