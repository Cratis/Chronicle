// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Records that one multi-write sink cycle committed, so a retry after a lost commit acknowledgment can tell that
/// the cycle already applied instead of applying it a second time.
/// </summary>
/// <param name="Id">The identifier of the call, stable across every retry of that call.</param>
/// <param name="TableName">The read-model table the cycle wrote to.</param>
/// <param name="EventSequenceNumber">The event sequence number the cycle applied.</param>
/// <param name="CreatedAt">Unix milliseconds on the database clock, set by the database when the row is inserted.</param>
internal record SinkCycleReceipt(Guid Id, string TableName, long EventSequenceNumber, long CreatedAt)
{
    /// <summary>
    /// Gets the SQL expression for the database clock, in Unix milliseconds, for a provider.
    /// </summary>
    /// <param name="databaseType">The <see cref="DatabaseType"/>.</param>
    /// <returns>The scalar SQL expression.</returns>
    public static string NowSql(DatabaseType databaseType) => databaseType switch
    {
        DatabaseType.PostgreSql => "(extract(epoch from clock_timestamp()) * 1000)::bigint",
        DatabaseType.SqlServer => "DATEDIFF_BIG(MILLISECOND, '1970-01-01', SYSUTCDATETIME())",
        _ => "CAST(strftime('%s', 'now') AS INTEGER) * 1000"
    };
}
