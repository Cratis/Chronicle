// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json;

/// <summary>
/// Holds the values of the <c language="json">date-time</c> format so that an explicit UTC offset stays part of the value.
/// </summary>
/// <remarks>
/// A <see cref="DateTime"/> cannot carry an offset: a value with one is turned into the host's local time, and every
/// serializer then writes the host's offset instead of the original one. A <c language="json">date-time</c> value with an explicit,
/// non-<c language="json">Z</c> offset is therefore held as a <see cref="DateTimeOffset"/>, which every serializer writes with its own
/// offset. A <c language="json">Z</c> value or one without an offset is held as a <see cref="DateTime"/>, as before.
/// </remarks>
public static class DateTimeValues
{
    static readonly TypeFormats _typeFormats = new();
    static readonly ConditionalWeakTable<JsonSchema, StrongBox<bool>> _holdsDateTimes = [];

    /// <summary>
    /// Gets the value to hold for a <c language="json">date-time</c> value read as a <see cref="DateTime"/>.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> read.</param>
    /// <param name="text">The text the value was read from.</param>
    /// <returns>A <see cref="DateTimeOffset"/> for a value with an explicit offset; otherwise the <see cref="DateTime"/>.</returns>
    public static object FromText(DateTime dateTime, string text) =>
        HasExplicitOffset(text) && TryParseWithOffset(text, out var dateTimeOffset) ? dateTimeOffset : (object)dateTime;

    /// <summary>
    /// Gets the value to hold for a <c language="json">date-time</c> value read by a <see cref="Utf8JsonReader"/>.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> read.</param>
    /// <param name="reader">The reader positioned on the value.</param>
    /// <returns>A <see cref="DateTimeOffset"/> for a value with an explicit offset; otherwise the <see cref="DateTime"/>.</returns>
    public static object FromReader(DateTime dateTime, ref Utf8JsonReader reader) =>
        dateTime.Kind == DateTimeKind.Local && reader.TryGetDateTimeOffset(out var dateTimeOffset) ? dateTimeOffset : (object)dateTime;

    /// <summary>
    /// Creates the JSON value for a <c language="json">date-time</c> value held as a <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="dateTimeOffset">The value.</param>
    /// <returns>A JSON value that keeps the offset and reads back as a string, a <see cref="DateTime"/> or a <see cref="DateTimeOffset"/>.</returns>
    public static JsonValue ToJsonValue(DateTimeOffset dateTimeOffset) =>
        JsonValue.Create(JsonSerializer.SerializeToElement(dateTimeOffset))!;

    /// <summary>
    /// Gets the value a <c language="json">date-time</c> value had before offsets were kept: the host's local time.
    /// </summary>
    /// <param name="value">The value as held.</param>
    /// <returns>The local <see cref="DateTime"/> for a <see cref="DateTimeOffset"/>; otherwise the value as it is.</returns>
    public static object? AsHeldBeforeOffsetsWereKept(object? value) =>
        value is DateTimeOffset dateTimeOffset ? dateTimeOffset.LocalDateTime : value;

    /// <summary>
    /// Gets content with its <c language="json">date-time</c> values as they were held before offsets were kept: the host's local time.
    /// </summary>
    /// <remarks>
    /// Only values of properties the schema declares as <c language="json">date-time</c> change, also in nested objects and
    /// collections; a <c language="json">date-time-offset</c> value is left as it is. The content given is never changed: a copy is
    /// returned when there is something to change, otherwise the same instance.
    /// </remarks>
    /// <param name="content">The content as held.</param>
    /// <param name="schema">The schema of the content.</param>
    /// <returns>The content with <c language="json">date-time</c> values held as before.</returns>
    public static ExpandoObject AsHeldBeforeOffsetsWereKept(ExpandoObject content, JsonSchema schema) =>
        HoldsDateTimes(schema.ActualTypeSchema) ? RestoreObject(content, schema.ActualTypeSchema) ?? content : content;

    static ExpandoObject? RestoreObject(ExpandoObject content, JsonSchema schema)
    {
        var properties = schema.FlattenedPropertiesByExactName;
        ExpandoObject? restored = null;
        foreach (var (name, value) in content)
        {
            if (!properties.TryGetValue(name, out var property) || Restore(value, property) is not { } restoredValue)
            {
                continue;
            }

            restored ??= Copy(content);
            ((IDictionary<string, object?>)restored)[name] = restoredValue;
        }

        return restored;
    }

    static object? Restore(object? value, JsonSchema schema) => value switch
    {
        DateTimeOffset dateTimeOffset when IsDateTime(schema) => dateTimeOffset.LocalDateTime,
        ExpandoObject nested when !schema.IsDictionary => RestoreObject(nested, schema.ActualTypeSchema),
        object?[] items when schema.Item is not null => RestoreItems(items, schema.Item),
        _ => null
    };

    static object?[]? RestoreItems(object?[] items, JsonSchema itemSchema)
    {
        object?[]? restored = null;
        for (var index = 0; index < items.Length; index++)
        {
            if (Restore(items[index], itemSchema) is not { } restoredItem)
            {
                continue;
            }

            restored ??= (object?[])items.Clone();
            restored[index] = restoredItem;
        }

        return restored;
    }

    static ExpandoObject Copy(ExpandoObject content)
    {
        var copy = new ExpandoObject();
        var values = (IDictionary<string, object?>)copy;
        foreach (var (name, value) in content)
        {
            values[name] = value;
        }

        return copy;
    }

    static bool HoldsDateTimes(JsonSchema schema) => _holdsDateTimes.GetValue(schema, _ => new(HoldsDateTimes(_, []))).Value;

    static bool HoldsDateTimes(JsonSchema schema, HashSet<JsonSchema> visited) =>
        visited.Add(schema) &&
        (IsDateTime(schema) ||
            (schema.Item is not null && HoldsDateTimes(schema.Item.ActualTypeSchema, visited)) ||
            schema.FlattenedProperties.Any(property => IsDateTime(property) || HoldsDateTimes(property.ActualTypeSchema, visited)));

    static bool IsDateTime(JsonSchema schema)
    {
        var format = schema.Format ?? schema.ActualTypeSchema.Format;
        if (string.IsNullOrEmpty(format) || !_typeFormats.IsKnown(format))
        {
            return false;
        }

        var type = _typeFormats.GetTypeForFormat(format);
        return (Nullable.GetUnderlyingType(type) ?? type) == typeof(DateTime);
    }

    static bool HasExplicitOffset(string text) =>
        DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed) && parsed.Kind == DateTimeKind.Local;

    static bool TryParseWithOffset(string text, out DateTimeOffset dateTimeOffset) =>
        DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTimeOffset);
}
