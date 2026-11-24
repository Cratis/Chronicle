// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SinkProvenanceStorage.given;

public class SchemaUpgradeCommands : DbCommandInterceptor, IDbConnectionInterceptor
{
    public bool Readable { get; set; } = true;
    public bool Legacy { get; set; } = true;
    public bool Imported { get; set; }
    public int Version { get; set; } = 1;
    public int LegacyReads { get; private set; }

    public ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(InterceptionResult.Suppress());

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        var presence = command.CommandText.Contains("HAS_PERMS_BY_NAME", StringComparison.Ordinal);
        var legacy = presence || command.CommandText.Contains("\"public\".", StringComparison.Ordinal) || command.CommandText.Contains("[dbo].", StringComparison.Ordinal);
        if (legacy)
        {
            LegacyReads++;
            if (!Readable) throw new InvalidOperationException("legacy metadata denied");
        }

        var table = new DataTable();
        if (presence)
        {
            table.Columns.Add("Value", typeof(int));
            table.Rows.Add(Legacy ? 1 : 0);
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }

        var inventory = command.CommandText.Contains("__cratis_sink_inventory", StringComparison.Ordinal);
        table.Columns.Add("Id", typeof(string));
        table.Columns.Add("Version", typeof(int));
        table.Columns.Add("Restricted", typeof(bool));
        if (inventory)
        {
            table.Columns.Add("DatabaseId", typeof(string));
            table.Columns.Add("InventoryVersion", typeof(int));
            table.Rows.Add("inventory", 1, false, "bb0263ce-eb39-4f34-bbd3-8f84c42ff526", 1);
        }
        else if (legacy ? Legacy : Imported)
        {
            table.Rows.Add("protected", legacy ? Version : 1, true);
        }

        OrderColumns(command, table);
        return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(1));

    static void OrderColumns(DbCommand command, DataTable table)
    {
        var columns = table.Columns.Cast<DataColumn>().OrderBy(column => Math.Max(
            command.CommandText.IndexOf($"\"{column.ColumnName}\"", StringComparison.Ordinal),
            command.CommandText.IndexOf($"[{column.ColumnName}]", StringComparison.Ordinal))).ToArray();
        for (var index = 0; index < columns.Length; index++) columns[index].SetOrdinal(index);
    }
}
