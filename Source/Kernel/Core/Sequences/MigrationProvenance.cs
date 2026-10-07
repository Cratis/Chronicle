// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Decides whether the migrations applied during content verification only carried protected values across opaquely.
/// </summary>
/// <remarks>
/// The real append runs migrations on encrypted content, while verification runs them on opaque markers. The two only
/// agree when a migration never looks inside a protected value. An operation that reads a protected value is
/// allowed only when it is a plain rename, move or copy of the whole value. Anything else - split, combine, value
/// map, default, any JMESPath expression or an operation kind this class does not know - could transform the real
/// ciphertext in a way a marker cannot reproduce, so the outcome of the comparison cannot be trusted.
/// </remarks>
static class MigrationProvenance
{
    /// <summary>
    /// Checks one migration step against the document it was applied to.
    /// </summary>
    /// <param name="operations">The migration operations (target property to expression).</param>
    /// <param name="input">The document the migration was applied to, with verification markers in place of protected values.</param>
    /// <returns>True when every protected value was only carried across opaquely; otherwise false.</returns>
    public static bool CarriesProtectedValuesOpaquely(JsonObject operations, JsonObject input)
    {
        if (!VerificationMarkers.AppearIn(input))
        {
            return true;
        }

        foreach (var (target, expression) in operations)
        {
            if (!IsOpaque(target, expression, input))
            {
                return false;
            }
        }

        return true;
    }

    static bool IsOpaque(string target, JsonNode? expression, JsonObject input)
    {
        if (expression is JsonValue plain && plain.TryGetValue<string>(out var path))
        {
            // A bare property path is a copy of the whole value. Anything else is an arbitrary JMESPath expression.
            return IsPropertyPath(path) || !VerificationMarkers.AppearIn(input);
        }

        if (expression is JsonObject builtIn && builtIn.Count == 1)
        {
            var (kind, configuration) = builtIn.Single();
            switch (kind)
            {
                case WellKnownExpressions.Rename:
                    return configuration is JsonValue;

                case WellKnownExpressions.Split:
                case WellKnownExpressions.MapValues:
                    return configuration is JsonObject single && !ReadsProtected(input, single["source"]);

                case WellKnownExpressions.Combine:
                    return configuration is JsonObject combine && combine["sources"] is JsonArray sources && !sources.Any(source => ReadsProtected(input, source));

                case WellKnownExpressions.DefaultValue:
                    return !ReadsProtected(input, JsonValue.Create(target));
            }
        }

        // Unknown operation kind, or a structure this class does not understand: only safe when nothing is protected.
        return !VerificationMarkers.AppearIn(input);
    }

    static bool ReadsProtected(JsonObject input, JsonNode? sourceProperty) =>
        sourceProperty is JsonValue value &&
        value.TryGetValue<string>(out var path) &&
        JsonPropertyPaths.TryResolve(input, path, out var node) &&
        VerificationMarkers.AppearIn(node);

    static bool IsPropertyPath(string path) =>
        path.Length > 0 &&
        path.Split('.').All(segment => segment.Length > 0 && !char.IsDigit(segment[0]) && segment.All(character => char.IsAsciiLetterOrDigit(character) || character == '_'));
}
