// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// JSON converter for <see cref="SourceAuthorization"/>.
/// </summary>
/// <remarks>
/// Reading fails on a missing, unknown or differently cased type discriminator rather than falling back to
/// <see cref="SourceAuthorization.None"/>, so that authored credentials are never silently dropped.
/// </remarks>
public class SourceAuthorizationJsonConverter : JsonConverter<SourceAuthorization>
{
    /// <inheritdoc/>
    public override SourceAuthorization? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected a source authorization object, but found {reader.TokenType}.");
        }

        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;

        if (!root.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("Source authorization is missing its 'type' discriminator.");
        }

        return typeElement.GetString() switch
        {
            "basic" => new SourceBasicAuthorization(
                root.GetProperty("username").GetString()!,
                root.GetProperty("password").GetString()!),
            "bearer" => new SourceBearerTokenAuthorization(
                root.GetProperty("token").GetString()!),
            "oauth" => new SourceOAuthAuthorization(
                root.GetProperty("authority").GetString()!,
                root.GetProperty("clientId").GetString()!,
                root.GetProperty("clientSecret").GetString()!),
            "none" => SourceAuthorization.None,
            _ => throw new JsonException("Unknown source authorization type. Supported types are 'basic', 'bearer', 'oauth' and 'none', in lower case.")
        };
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, SourceAuthorization value, JsonSerializerOptions options) =>
        value.Switch(
            basic =>
            {
                writer.WriteStartObject();
                writer.WriteString("type", "basic");
                writer.WriteString("username", basic.Username);
                writer.WriteString("password", basic.Password);
                writer.WriteEndObject();
            },
            bearer =>
            {
                writer.WriteStartObject();
                writer.WriteString("type", "bearer");
                writer.WriteString("token", bearer.Token);
                writer.WriteEndObject();
            },
            oauth =>
            {
                writer.WriteStartObject();
                writer.WriteString("type", "oauth");
                writer.WriteString("authority", oauth.Authority);
                writer.WriteString("clientId", oauth.ClientId);
                writer.WriteString("clientSecret", oauth.ClientSecret);
                writer.WriteEndObject();
            },
            none =>
            {
                writer.WriteStartObject();
                writer.WriteString("type", "none");
                writer.WriteEndObject();
            });
}
