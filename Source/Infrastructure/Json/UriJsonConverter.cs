// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Json;

/// <summary>
/// Represents a <see cref="JsonConverter{T}"/> that can convert to and from <see cref="Uri"/>.
/// </summary>
public class UriJsonConverter : JsonConverter<Uri>
{
    /// <inheritdoc/>
    public override Uri? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var json = JsonDocument.ParseValue(ref reader);
        var root = json.RootElement;

        return new Uri(root.GetString()!);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Uri value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
