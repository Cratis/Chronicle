// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Concepts.Observation.Webhooks;

/// <summary>
/// JSON converter for <see cref="WebhookAuthorization"/>.
/// </summary>
public class WebhookAuthorizationJsonConverter : JsonConverter<WebhookAuthorization>
{
    /// <inheritdoc/>
    public override WebhookAuthorization? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            // Read the entire object
            var jsonDoc = JsonDocument.ParseValue(ref reader);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("type", out var typeElement))
            {
                var authType = typeElement.GetString();
                return authType switch
                {
                    "basic" => new BasicAuthorization(
                        root.GetProperty("username").GetString()!,
                        root.GetProperty("password").GetString()!),
                    "bearer" => new BearerTokenAuthorization(
                        root.GetProperty("token").GetString()!),
                    "oauth" => new OAuthAuthorization(
                        root.GetProperty("authority").GetString()!,
                        root.GetProperty("clientId").GetString()!,
                        root.GetProperty("clientSecret").GetString()!),
                    _ => WebhookAuthorization.None
                };
            }
        }

        return WebhookAuthorization.None;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, WebhookAuthorization value, JsonSerializerOptions options)
    {
        // Serialize using the Switch method to avoid accessing variant properties
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
}
