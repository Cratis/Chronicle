// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using Cratis.Json;

namespace Cratis.Chronicle.Storage.Imports;

/// <summary>
/// Holds the serialization every provider uses for import records and staged chunks, so they all round-trip alike.
/// </summary>
public static class ImportStorageSerialization
{
    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> for import staging.
    /// </summary>
    /// <remarks>
    /// Event content is JSON held in a string, so the default encoder's escaping of every quote as six characters would
    /// make a staged chunk several times its content. Relaxed escaping only escapes what JSON requires; the payload is
    /// stored, never rendered as HTML.
    /// </remarks>
    public static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new EnumerableConceptAsJsonConverterFactory(), new ConceptAsJsonConverterFactory() }
    };

    /// <summary>
    /// Serializes an import record.
    /// </summary>
    /// <param name="record">The <see cref="ImportRecord"/>.</param>
    /// <returns>The JSON.</returns>
    public static string Serialize(ImportRecord record) => JsonSerializer.Serialize(record, Options);

    /// <summary>
    /// Serializes a staged chunk.
    /// </summary>
    /// <param name="chunk">The <see cref="StagedImportChunk"/>.</param>
    /// <returns>The JSON.</returns>
    public static string Serialize(StagedImportChunk chunk) => JsonSerializer.Serialize(chunk, Options);

    /// <summary>
    /// Deserializes an import record.
    /// </summary>
    /// <param name="json">The JSON.</param>
    /// <returns>The <see cref="ImportRecord"/>.</returns>
    public static ImportRecord DeserializeRecord(string json) => JsonSerializer.Deserialize<ImportRecord>(json, Options)!;

    /// <summary>
    /// Deserializes a staged chunk.
    /// </summary>
    /// <param name="json">The JSON.</param>
    /// <returns>The <see cref="StagedImportChunk"/>.</returns>
    public static StagedImportChunk DeserializeChunk(string json) => JsonSerializer.Deserialize<StagedImportChunk>(json, Options)!;
}
