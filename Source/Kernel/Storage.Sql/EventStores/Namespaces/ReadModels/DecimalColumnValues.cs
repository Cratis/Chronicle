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
    /// Rounds to SQL Server's column scale and refuses integer overflow.
    /// </summary>
    /// <param name="value">The decimal value.</param>
    /// <returns>The value rounded to 18 fractional digits, with midpoints rounded away from zero.</returns>
    /// <exception cref="DecimalValueExceedsColumnPrecision">The integer part cannot fit the column.</exception>
    internal static decimal ForSqlServer(decimal value)
    {
        if (value <= -100000000000000000000m || value >= 100000000000000000000m)
        {
            throw new DecimalValueExceedsColumnPrecision(value);
        }
        return decimal.Round(value, 18, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Removes the redundant trailing zeros introduced by fixed-scale columns.
    /// </summary>
    /// <param name="value">The stored value.</param>
    /// <returns>The value without redundant trailing zeros.</returns>
    internal static decimal FromSqlServer(decimal value) => decimal.Parse(value.ToString("G29", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);
}
