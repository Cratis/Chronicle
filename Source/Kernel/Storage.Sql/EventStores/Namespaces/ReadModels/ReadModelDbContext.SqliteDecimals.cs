// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

public partial class ReadModelDbContext
{
    static readonly ValueConverter<decimal, string> _sqliteTextDecimalConverter = new(
        value => value.ToString(CultureInfo.InvariantCulture),
        value => decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture));

    static readonly ValueConverter<decimal, double> _sqliteDecimalConverter = new(
        value => (double)value,
        value => FromSqliteDecimal(value));

    bool _columnMappingsPrepared;

    /// <summary>
    /// Gets the existing SQLite columns that retain decimal text conversion.
    /// </summary>
    internal IReadOnlySet<string> TextDecimalColumns { get; private set; } = new HashSet<string>();

    /// <summary>
    /// Selects SQLite decimal conversion from physical columns before EF builds its model.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal async Task PrepareColumnMappings()
    {
        if (_columnMappingsPrepared) return;
        TextDecimalColumns = await SqliteDecimalColumns.Read(_mappingOptions, TableName, Columns);
        _columnMappingsPrepared = true;
    }

    static decimal FromSqliteDecimal(double value)
    {
        if (value >= (double)decimal.MaxValue) return decimal.MaxValue;
        if (value <= (double)decimal.MinValue) return decimal.MinValue;
        return (decimal)value;
    }
}
