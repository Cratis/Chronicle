// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Checks SQL Server nonclustered index key limits.
/// </summary>
internal static class SqlServerIndexKeyBudget
{
    /// <summary>
    /// Checks a complete plan against existing SQL Server column sizes.
    /// </summary>
    /// <param name="context">The read-model context.</param>
    /// <param name="table">The table name.</param>
    /// <param name="plan">The resolved index plan.</param>
    /// <returns>The rejection reason, or null when the plan is supported.</returns>
    internal static async Task<string?> RejectionReason(ReadModelDbContext context, string table, ReadModelIndexPlan plan)
    {
        if (context.Database.GetDatabaseType() != DatabaseType.SqlServer) return null;
        var bytes = 0;
        foreach (var target in plan.Targets)
        {
            if (target.IsExpression)
            {
                bytes += 900;
                continue;
            }
            var column = target.Column;
            var length = await context.Database.SqlQuery<int>($"SELECT CAST(max_length AS int) AS [Value] FROM sys.columns WHERE object_id=OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({table})) AND name={column.Name}").SingleAsync();
            if (column.ClrType == typeof(string) && length is < 1 or > 1700)
            {
                return "the existing string column is unbounded or exceeds the SQL Server nonclustered key limit of 1700 bytes; replay to apply a supported bound";
            }
            bytes += length;
        }
        if (bytes > 1700)
        {
            return "the combined key exceeds the SQL Server nonclustered key limit of 1700 bytes";
        }
        return null;
    }
}
