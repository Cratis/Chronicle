// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Computes the signature that identifies a projection registration by what it contains.
/// </summary>
/// <remarks>
/// Two registrations have the same signature exactly when they carry the same registrant, version and full-set owner
/// and the same definitions, whatever order the definitions and their dictionaries were submitted in. The signature is
/// computed from the content received, never from anything the client claims about it. Every property of a definition
/// takes part except <see cref="ProjectionDefinition.LastUpdated"/>, which is a timestamp the receiver controls, so a
/// property added to a definition is part of the signature without anyone remembering to add it. The signature is only
/// ever compared within one process, so it is not persisted and needs no stability across versions.
/// </remarks>
public static class ProjectionRegistrationSignature
{
    /// <summary>
    /// Computes the signature of a registration.
    /// </summary>
    /// <param name="definitions">The submitted definitions.</param>
    /// <param name="fullSetOwner">The owner whose full set this is, if any.</param>
    /// <param name="registrant">The registering application, if any.</param>
    /// <param name="version">The registering application's version, if any.</param>
    /// <returns>The signature, as hexadecimal text.</returns>
    public static string For(IEnumerable<ProjectionDefinition> definitions, ProjectionOwner? fullSetOwner, string? registrant, string? version)
    {
        var buffer = new ArrayBufferWriter<byte>(16 * 1024);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("owner", fullSetOwner?.ToString());
            writer.WriteString("registrant", registrant);
            writer.WriteString("version", version);
            writer.WriteStartArray("definitions");
            foreach (var definition in definitions.OrderBy(definition => definition.Identifier.Value, StringComparer.Ordinal))
            {
                WriteCanonical(writer, JsonSerializer.SerializeToNode(definition with { LastUpdated = null }, ProjectionFactory.FingerprintOptions));
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Convert.ToHexString(SHA256.HashData(buffer.WrittenSpan));
    }

    static void WriteCanonical(Utf8JsonWriter writer, JsonNode? node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                writer.WriteStartObject();
                foreach (var property in jsonObject.OrderBy(property => property.Key, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Key);
                    WriteCanonical(writer, property.Value);
                }

                writer.WriteEndObject();
                break;

            case JsonArray jsonArray:
                writer.WriteStartArray();
                foreach (var item in jsonArray)
                {
                    WriteCanonical(writer, item);
                }

                writer.WriteEndArray();
                break;

            case null:
                writer.WriteNullValue();
                break;

            default:
                node.WriteTo(writer);
                break;
        }
    }
}
