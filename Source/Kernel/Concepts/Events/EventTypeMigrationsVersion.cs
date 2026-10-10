// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Security.Cryptography;
using System.Text.Json;

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Identifies the executable migration definitions by their canonical content, excluding descriptive operations.
/// </summary>
/// <param name="Value">The SHA-256 digest of the definitions.</param>
public record EventTypeMigrationsVersion(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the version for an empty migration set.
    /// </summary>
    public static readonly EventTypeMigrationsVersion None = new(string.Empty);

    /// <summary>
    /// Computes a stable version independently of registration and JSON property ordering.
    /// </summary>
    /// <param name="migrations">The migration definitions.</param>
    /// <returns>The content-addressed version.</returns>
    public static EventTypeMigrationsVersion For(IEnumerable<EventTypeMigrationDefinition> migrations)
    {
        var definitions = migrations.OrderBy(_ => _.FromGeneration.Value).ThenBy(_ => _.ToGeneration.Value).ToArray();
        if (definitions.Length == 0)
        {
            return None;
        }

        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        writer.WriteStartArray();
        foreach (var definition in definitions)
        {
            using var document = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                from = definition.FromGeneration.Value,
                to = definition.ToGeneration.Value,
                upcast = definition.UpcastJmesPath,
                downcast = definition.DowncastJmesPath
            }));
            WriteSorted(document.RootElement, writer);
        }
        writer.WriteEndArray();
        writer.Flush();

        return new(Convert.ToHexString(SHA256.HashData(buffer.WrittenSpan)));
    }

    static void WriteSorted(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(_ => _.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteSorted(property.Value, writer);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteSorted(item, writer);
                }
                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }
}
