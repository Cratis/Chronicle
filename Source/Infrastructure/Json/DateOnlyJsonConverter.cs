// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Json;

/// <summary>
/// Represents a <see cref="JsonConverter{T}"/> for handling <see cref="DateOnly"/>.
/// </summary>
public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    /// <inheritdoc/>
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TryGetDateTimeOffset(out var dateTimeOffset))
        {
            return DateOnly.FromDateTime(dateTimeOffset.DateTime);
        }

        if (reader.TryGetDateTime(out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        var dateFromString = DateTime.Parse(reader.GetString()!);
        return DateOnly.FromDateTime(dateFromString);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString("O"));
}
