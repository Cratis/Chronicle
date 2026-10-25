// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Encodes decimal values as fixed-width ASCII keys ordered numerically by SQLite BINARY collation.
/// </summary>
internal static class SqliteDecimalSortKey
{
    /// <summary>
    /// The reserved prefix for generated decimal ordering columns.
    /// </summary>
    internal const string Prefix = "__order_";

    /// <summary>
    /// Gets the generated ordering column for a decimal property.
    /// </summary>
    /// <param name="column">The original property name.</param>
    /// <returns>The internal ordering column name.</returns>
    internal static string ColumnNameFor(string column) => $"{Prefix}{column}";

    /// <summary>
    /// Determines whether a projected column is a scalar decimal, including nullable decimals.
    /// </summary>
    /// <param name="column">The projected column.</param>
    /// <returns>Whether the column needs a decimal ordering key.</returns>
    internal static bool IsDecimal(ProjectedColumn column) => !column.IsJson &&
        (Nullable.GetUnderlyingType(column.ClrType) ?? column.ClrType) == typeof(decimal);

    /// <summary>
    /// Builds a deterministic generated-column expression without floating-point conversions.
    /// </summary>
    /// <param name="delimitedColumn">The source identifier delimited by the provider.</param>
    /// <returns>The SQL expression yielding a canonical ordering key, or NULL for non-text values.</returns>
    internal static string SqlExpressionFor(string delimitedColumn)
    {
        var magnitude = $"ltrim({delimitedColumn}, '-')";
        var point = $"instr({magnitude}, '.')";
        var integer = $"CASE WHEN {point} > 0 THEN substr({magnitude}, 1, {point} - 1) ELSE {magnitude} END";
        var fraction = $"CASE WHEN {point} > 0 THEN substr({magnitude}, {point} + 1) ELSE '' END";
        var body = $"(substr('{new string('0', 29)}' || ({integer}), -29) || substr(({fraction}) || '{new string('0', 28)}', 1, 28))";
        var reversed = body;
        for (var digit = 0; digit < 10; digit++)
        {
            reversed = $"replace({reversed}, '{(char)('0' + digit)}', '{(char)('j' - digit)}')";
        }

        return $"CASE WHEN typeof({delimitedColumn}) <> 'text' OR {delimitedColumn} GLOB '*[^-0-9.]*' THEN NULL " +
            $"WHEN ltrim(({integer}) || ({fraction}), '0') = '' THEN '1{new string('0', 57)}' " +
            $"WHEN substr({delimitedColumn}, 1, 1) = '-' THEN '0' || {reversed} ELSE '1' || {body} END";
    }

    /// <summary>
    /// Encodes a decimal for comparison against a generated ordering column.
    /// </summary>
    /// <param name="value">The exact decimal value.</param>
    /// <returns>The fixed-width ASCII ordering key.</returns>
    internal static string For(decimal value)
    {
        if (value == 0m) return $"1{new string('0', 57)}";
        var parts = value.ToString(CultureInfo.InvariantCulture).TrimStart('-').Split('.');
        var body = parts[0].PadLeft(29, '0') + (parts.Length > 1 ? parts[1] : string.Empty).PadRight(28, '0');

        return value < 0m ? "0" + new string(body.Select(digit => (char)('j' - (digit - '0'))).ToArray()) : "1" + body;
    }
}
