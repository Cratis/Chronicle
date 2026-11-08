// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json;

/// <summary>
/// Defines a converter that can convert between a <see cref="JsonObject"/> and a <see cref="ExpandoObject"/> with a <see cref="JsonSchema"/> holding the type information.
/// </summary>
public interface IExpandoObjectConverter
{
    /// <summary>
    /// Convert a <see cref="JsonObject"/> to <see cref="ExpandoObject"/>.
    /// </summary>
    /// <param name="document"><see cref="JsonObject"/> to convert.</param>
    /// <param name="schema">The <see cref="JsonSchema"/> with the type information.</param>
    /// <returns>A new <see cref="ExpandoObject"/> instance.</returns>
    ExpandoObject ToExpandoObject(JsonObject document, JsonSchema schema);

    /// <summary>
    /// Convert a <see cref="ExpandoObject"/> to <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="expandoObject">The <see cref="ExpandoObject"/> to convert.</param>
    /// <param name="schema">The <see cref="JsonSchema"/> with the type information.</param>
    /// <returns>A new <see cref="JsonObject"/> instance.</returns>
    JsonObject ToJsonObject(ExpandoObject expandoObject, JsonSchema schema);

    /// <summary>
    /// Write an <see cref="ExpandoObject"/> as the JSON <see cref="ToJsonObject"/> would give, straight to a writer.
    /// </summary>
    /// <param name="writer">The <see cref="Utf8JsonWriter"/> to write to.</param>
    /// <param name="expandoObject">The <see cref="ExpandoObject"/> to write.</param>
    /// <param name="schema">The <see cref="JsonSchema"/> describing it.</param>
    /// <remarks>
    /// The implementation here builds the document first; a converter that can write as it goes should replace it,
    /// so that a large document is never held a second time as a tree of nodes.
    /// </remarks>
    void WriteJson(Utf8JsonWriter writer, ExpandoObject expandoObject, JsonSchema schema) => ToJsonObject(expandoObject, schema).WriteTo(writer);

    /// <summary>
    /// Read UTF-8 encoded JSON into an <see cref="ExpandoObject"/>, as <see cref="ToExpandoObject(JsonObject, JsonSchema)"/> would.
    /// </summary>
    /// <param name="utf8Json">The JSON to read.</param>
    /// <param name="schema">The <see cref="JsonSchema"/> describing it.</param>
    /// <returns>The <see cref="ExpandoObject"/>.</returns>
    /// <remarks>
    /// The implementation here builds the whole document first; a converter that can read as it goes should replace it.
    /// </remarks>
    ExpandoObject ToExpandoObject(ReadOnlyMemory<byte> utf8Json, JsonSchema schema) => ToExpandoObject(JsonNode.Parse(utf8Json.Span)!.AsObject(), schema);
}
