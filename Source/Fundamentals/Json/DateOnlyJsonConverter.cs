// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aksio.Cratis.Json;

/// <summary>
/// Represents a <see cref="JsonConverter{T}"/> for handling <see cref="DateOnly"/>.
/// </summary>
public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    /// <inheritdoc/>
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => DateOnly.Parse(reader.GetString()!);

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString("O"));
}
