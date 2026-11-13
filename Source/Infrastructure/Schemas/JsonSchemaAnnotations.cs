// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Reports and removes obsolete writer annotations without changing schema constraints or payload data.
/// </summary>
public static class JsonSchemaAnnotations
{
    static readonly string[] _cleanupKeywords = ["deprecated", "contentEncoding", "examples", "$comment"];

    /// <summary>
    /// Finds cleanup keywords at schema declarations, excluding property names and arbitrary data.
    /// </summary>
    /// <param name="schema">The schema root.</param>
    /// <returns>Keywords and their escaped schema paths.</returns>
    public static IReadOnlyList<(string Keyword, string Path)> FindAnnotationKeywords(JsonNode schema) =>
        [.. DefaultOnlyPropertyRefinement.Pairs(schema, schema)
            .SelectMany(pair => _cleanupKeywords.Where(pair.Stored.ContainsKey)
                .Select(keyword => (keyword, $"{pair.Path}/{keyword}")))];

    /// <summary>
    /// Creates a copy without cleanup annotations. Titles and descriptions are retained.
    /// </summary>
    /// <param name="schema">The schema root. It is not modified.</param>
    /// <returns>The normalized schema.</returns>
    public static JsonNode RemoveAnnotations(JsonNode schema)
    {
        var normalized = schema.DeepClone();
        RemoveKeywords(normalized, _cleanupKeywords);
        return normalized;
    }

    /// <summary>
    /// Removes the selected keywords using the shared schema-declaration walker.
    /// </summary>
    /// <param name="schema">The schema to modify.</param>
    /// <param name="keywords">The keywords to remove.</param>
    internal static void RemoveKeywords(JsonNode? schema, IEnumerable<string> keywords) =>
        DefaultOnlyPropertyRefinement.Normalize(schema, declaration =>
        {
            foreach (var keyword in keywords)
            {
                declaration.Remove(keyword);
            }
        });
}
