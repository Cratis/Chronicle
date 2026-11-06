// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Json;

/// <summary>
/// Reading and writing JSON documents that are megabytes in size, for instance the state of a reducer.
/// </summary>
/// <remarks>
/// The framework's own string based entry points - <see cref="JsonNode.ToJsonString(JsonSerializerOptions?)"/> and
/// <see cref="JsonNode.Parse(string, JsonNodeOptions?, JsonDocumentOptions)"/> - work in buffers rented from the shared
/// array pool: the writer rents a buffer that doubles each time it fills, and the parser rents three bytes for every
/// character. For a large document that rents a buffer of every size up to tens of megabytes, every one of which goes
/// back to the pool and stays there, per thread, until the pool trims it. That retained memory is what makes a
/// heap limit run out while the document itself is nowhere near it. The methods here allocate exactly what they
/// need, which the garbage collector reclaims as soon as they are done with.
/// </remarks>
public static class LargeJson
{
    const int InitialCapacity = 16 * 1024;

    /// <summary>
    /// Serialize a node to a compact JSON string.
    /// </summary>
    /// <param name="node">The <see cref="JsonNode"/> to serialize.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to serialize with.</param>
    /// <param name="sizeHint">The expected size of the JSON in bytes, for instance the size of the previous version of the same document. Zero when unknown.</param>
    /// <returns>The JSON.</returns>
    public static string Serialize(JsonNode node, JsonSerializerOptions options, int sizeHint = 0)
    {
        var buffer = new ArrayBufferWriter<byte>(Math.Max(InitialCapacity, sizeHint));
        using (var writer = new Utf8JsonWriter(
            buffer,
            new JsonWriterOptions
            {
                Encoder = options.Encoder,
                Indented = options.WriteIndented,
                SkipValidation = true
            }))
        {
            node.WriteTo(writer, options);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Parse a JSON string into a node.
    /// </summary>
    /// <param name="json">The JSON to parse.</param>
    /// <returns>The parsed <see cref="JsonNode"/>.</returns>
    public static JsonNode? Parse(string json)
    {
        var bytes = new byte[Encoding.UTF8.GetByteCount(json)];
        var length = Encoding.UTF8.GetBytes(json, bytes);
        return JsonNode.Parse(bytes.AsSpan(0, length));
    }
}
