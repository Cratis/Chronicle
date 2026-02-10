// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Concepts.Settings;

/// <summary>
/// JSON converter for <see cref="LanguageModelProvider"/>.
/// </summary>
public class LanguageModelProviderJsonConverter : JsonConverter<LanguageModelProvider>
{
    /// <inheritdoc/>
    public override LanguageModelProvider? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var jsonDoc = JsonDocument.ParseValue(ref reader);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("type", out var typeElement))
            {
                var providerType = typeElement.GetString();
                return providerType switch
                {
                    "openai" => new OpenAIProvider(
                        new Uri(root.GetProperty("endpoint").GetString()!),
                        new ApiKey(root.GetProperty("apiKey").GetString()!),
                        new LanguageModel(root.GetProperty("model").GetString()!)),
                    _ => LanguageModelProvider.None
                };
            }
        }

        return LanguageModelProvider.None;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, LanguageModelProvider value, JsonSerializerOptions options)
    {
        value.Switch(
            openai =>
            {
                writer.WriteStartObject();
                writer.WriteString("type", "openai");
                writer.WriteString("endpoint", openai.Endpoint.ToString());
                writer.WriteString("apiKey", openai.ApiKey.Value);
                writer.WriteString("model", openai.Model.Value);
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
