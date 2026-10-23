// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Builds deterministic MongoDB keys for ordered index definitions.
/// </summary>
internal static class ReadModelIndexKeys
{
    /// <summary>
    /// Builds the complete ordered key document.
    /// </summary>
    /// <param name="index">The index definition.</param>
    /// <returns>The MongoDB key document.</returns>
    internal static BsonDocument For(IndexDefinition index) => new(index.Properties.Select(property => new BsonElement(property.Path.Path, property.Descending ? -1 : 1)));

    /// <summary>
    /// Gets the stable managed index name.
    /// </summary>
    /// <param name="index">The index definition.</param>
    /// <returns>The index name.</returns>
    internal static string Name(IndexDefinition index)
    {
        if (!index.IsComposite && !index.Properties[0].Descending)
        {
            return "chronicle_idx_" + index.Properties[0].Path.Path.Replace('.', '_');
        }
        return "chronicle_idx_v2_" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(index.Signature)))[..16];
    }

    /// <summary>
    /// Refuses compound indexes spanning multiple array paths.
    /// </summary>
    /// <param name="index">The index definition.</param>
    /// <param name="schema">The read-model schema.</param>
    /// <exception cref="InvalidOperationException">The index contains multiple array paths.</exception>
    internal static void Validate(IndexDefinition index, JsonSchema schema)
    {
        if (!ArrayPathsFormAChain(index.Properties.SelectMany(property => ArrayPaths(property.Path.Path, schema))))
        {
            throw new InvalidOperationException("A compound read-model index cannot contain multiple array paths.");
        }
    }

    /// <summary>
    /// Determines whether array paths nest as one chain, where each path is a segment-wise prefix of the next.
    /// </summary>
    /// <param name="arrayPaths">The array paths.</param>
    /// <returns>True when the paths form a single chain.</returns>
    internal static bool ArrayPathsFormAChain(IEnumerable<string> arrayPaths)
    {
        var ordered = arrayPaths.Distinct(StringComparer.Ordinal).Select(path => path.Split('.')).OrderBy(segments => segments.Length).ToArray();
        for (var index = 1; index < ordered.Length; index++)
        {
            if (!ordered[index].Take(ordered[index - 1].Length).SequenceEqual(ordered[index - 1], StringComparer.Ordinal)) return false;
        }
        return true;
    }

    static IEnumerable<string> ArrayPaths(string path, JsonSchema schema)
    {
        var current = schema.ActualSchema;
        var prefix = new List<string>();
        foreach (var segment in path.Split('.'))
        {
            if (current.Type.HasFlag(JsonObjectType.Array))
            {
                yield return string.Join('.', prefix);
                if (current.Item is null) yield break;
                current = current.Item.ActualSchema;
            }
            var child = current.GetFlattenedProperties().FirstOrDefault(property => property.Name == segment);
            if (child is null) yield break;
            prefix.Add(segment);
            current = child.ActualSchema;
        }
        if (current.Type.HasFlag(JsonObjectType.Array)) yield return string.Join('.', prefix);
    }
}
