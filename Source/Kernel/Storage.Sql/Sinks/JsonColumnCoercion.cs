// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Coerces values into the CLR types of typed read model columns, and reads values stored as text back into their types.
/// </summary>
internal static class JsonColumnCoercion
{
    /// <summary>
    /// Coerce a value into the CLR type of a column, leaving it as it is when it cannot be converted.
    /// </summary>
    /// <param name="column">The <see cref="ProjectedColumn"/> the value is for.</param>
    /// <param name="value">The value to coerce.</param>
    /// <returns>The coerced value.</returns>
    internal static object? CoerceForColumn(ProjectedColumn column, object? value)
    {
        if (value is null)
        {
            return null;
        }

        value = JsonColumnDocuments.UnwrapConceptValue(value);

        var clr = Nullable.GetUnderlyingType(column.ClrType) ?? column.ClrType;
        if (clr.IsInstanceOfType(value))
        {
            return value;
        }

        try
        {
            return ConvertToClrType(clr, value!);
        }
        catch
        {
            return value;
        }
    }

    /// <summary>
    /// Check whether a type is stored as text in a JSON document.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if it is stored as text, false if not.</returns>
    internal static bool IsStoredAsText(Type type) =>
        type == typeof(Guid) || type == typeof(DateTimeOffset) || type == typeof(DateTime) || type == typeof(DateOnly) || type == typeof(TimeOnly);

    /// <summary>
    /// Read text stored for a type back into the type, leaving it as text when it is malformed.
    /// </summary>
    /// <param name="type">The type the text is stored for.</param>
    /// <param name="text">The stored text.</param>
    /// <returns>The converted value, or the text.</returns>
    internal static object TryConvertStoredText(Type type, string text)
    {
        try
        {
            return ConvertToClrType(type, text) ?? text;
        }
        catch (FormatException)
        {
            return text;
        }
    }

    static object? ConvertToClrType(Type clr, object value)
    {
        if (clr.IsEnum)
        {
            return Convert.ChangeType(value, Enum.GetUnderlyingType(clr), CultureInfo.InvariantCulture);
        }

        if (clr == typeof(Guid))
        {
            return value is string s ? Guid.Parse(s) : value;
        }

        if (value is string text && ParseTemporalText(clr, text) is { } parsed)
        {
            return parsed;
        }

        if (clr == typeof(decimal))
        {
            return DecimalValues.ToDecimal(value);
        }

        return Convert.ChangeType(value, clr, CultureInfo.InvariantCulture);
    }

    static object? ParseTemporalText(Type clr, string text)
    {
        if (clr == typeof(DateTime))
        {
            return DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        if (clr == typeof(DateTimeOffset))
        {
            return DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        if (clr == typeof(DateOnly))
        {
            return DateOnly.Parse(text, CultureInfo.InvariantCulture);
        }

        return clr == typeof(TimeOnly) ? TimeOnly.Parse(text, CultureInfo.InvariantCulture) : null;
    }
}
