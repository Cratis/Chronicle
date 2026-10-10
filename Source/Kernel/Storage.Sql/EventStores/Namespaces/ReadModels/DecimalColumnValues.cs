// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Preserves decimal precision at the SQL Server column boundary.
/// </summary>
internal static class DecimalColumnValues
{
    /// <summary>
    /// Refuses values that SQL Server would round or overflow.
    /// </summary>
    /// <param name="value">The decimal value.</param>
    /// <returns>The value unchanged.</returns>
    /// <exception cref="DecimalValueExceedsColumnPrecision">The value cannot be stored exactly.</exception>
    internal static decimal ForSqlServer(decimal value)
    {
        if (decimal.Round(value, 18) != value || value <= -100000000000000000000m || value >= 100000000000000000000m)
        {
            throw new DecimalValueExceedsColumnPrecision(value);
        }
        return value;
    }

    /// <summary>
    /// Removes the redundant trailing zeros introduced by fixed-scale columns.
    /// </summary>
    /// <param name="value">The stored value.</param>
    /// <returns>The value without redundant trailing zeros.</returns>
    internal static decimal FromSqlServer(decimal value) => decimal.Parse(value.ToString("G29", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);
}
