// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Json;

/// <summary>
/// Represents a <see cref="JsonConverter{T}"/> that can convert <see cref="Type"/>.
/// </summary>
public class TypeJsonConverter : JsonConverter<Type>
{
    /// <inheritdoc/>
    public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString() ?? throw new JsonException("Expected string");
        return Type.GetType(value)!;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"{value.FullName}, {value.Assembly.GetName().Name}");
    }
}
