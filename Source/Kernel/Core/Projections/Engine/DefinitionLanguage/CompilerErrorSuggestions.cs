// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

/// <summary>
/// Suggests registered spellings in diagnostics without changing exact name resolution.
/// </summary>
internal static class CompilerErrorSuggestions
{
    /// <summary>
    /// Suggests full identifiers matching a title, short type name, or identifier casing.
    /// </summary>
    /// <param name="name">The unresolved read-model name.</param>
    /// <param name="definitions">The registered read models.</param>
    /// <returns>The diagnostic suffix, or an empty string if there are no candidates.</returns>
    internal static string ForReadModel(string name, IEnumerable<ReadModelDefinition> definitions) => Format(
        definitions.Where(definition =>
            string.Equals(definition.Identifier.Value, name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(definition.Identifier.Value.Split(['.', '+'])[^1], name, StringComparison.OrdinalIgnoreCase) ||
            (definition.Schemas.Count > 0 && string.Equals(definition.GetSchemaForLatestGeneration().Title, name, StringComparison.OrdinalIgnoreCase)))
        .Select(definition => definition.Identifier.Value));

    /// <summary>
    /// Suggests property paths matching the unresolved path case-insensitively.
    /// </summary>
    /// <param name="path">The unresolved property path.</param>
    /// <param name="schema">The schema containing the property.</param>
    /// <returns>The diagnostic suffix, or an empty string if there are no candidates.</returns>
    internal static string ForProperty(string path, JsonSchema schema) => Format(FindPropertyPaths(path.Split('.'), schema, string.Empty));

    static IEnumerable<string> FindPropertyPaths(string[] parts, JsonSchema schema, string prefix) =>
        schema.ActualSchema.Properties
            .Where(_ => string.Equals(_.Key, parts[0], StringComparison.OrdinalIgnoreCase))
            .SelectMany(_ => FindPropertyPathsFrom(parts, _.Value, prefix + _.Key));

    static IEnumerable<string> FindPropertyPathsFrom(string[] parts, JsonSchemaProperty property, string path) =>
        parts.Length switch
        {
            1 => [path],
            _ when property.ActualSchema.Type.HasFlag(JsonObjectType.Object) => FindPropertyPaths(parts[1..], property.ActualSchema, path + "."),
            _ => []
        };

    static string Format(IEnumerable<string> candidates)
    {
        var names = candidates.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        return names.Length switch
        {
            0 => string.Empty,
            1 => $"; did you mean '{names[0]}'?",
            _ => $"; did you mean one of {string.Join(", ", names.Select(name => $"'{name}'"))}?"
        };
    }
}
