// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using NJsonSchema;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Represents a JSON converter for <see cref="JsonSchema"/>.
/// </summary>
internal sealed class JsonSchemaConverter : JsonConverter<JsonSchema>
{
    /// <inheritdoc/>
    public override JsonSchema? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonSchemaString = reader.GetString();
        if (jsonSchemaString is not null)
        {
            return JsonSchema.FromJsonAsync(jsonSchemaString).GetAwaiter().GetResult();
        }

        return null;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, JsonSchema value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToJson());
}
