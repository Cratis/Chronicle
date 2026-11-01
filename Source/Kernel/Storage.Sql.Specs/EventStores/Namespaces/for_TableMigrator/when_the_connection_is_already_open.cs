// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_TableMigrator;

public class when_the_connection_is_already_open : given.a_sqlite_migration_context
{
    bool _columnExists;
    ConnectionState _afterExecute;
    ConnectionState _afterColumn;
    ConnectionState _afterTable;
    ConnectionState _afterColumns;

    async Task Because()
    {
        await _connection.OpenAsync();
        await _migrator.ExecuteMigrationOperations(_context, CreateTable());
        _afterExecute = _connection.State;
        _columnExists = await _migrator.ColumnExists(_context, "probe", "id");
        _afterColumn = _connection.State;
        await _migrator.EnsureTableMigrated("orders", _context, CreateOrders);
        _afterTable = _connection.State;
        await _readModelMigrator.EnsureTableMigrated("orders", _context);
        _afterColumns = _connection.State;
    }

    [Fact] void should_run_the_migration() => _columnExists.ShouldBeTrue();
    [Fact] void should_leave_the_connection_as_found_after_executing() => _afterExecute.ShouldEqual(ConnectionState.Open);
    [Fact] void should_leave_the_connection_as_found_after_checking_a_column() => _afterColumn.ShouldEqual(ConnectionState.Open);
    [Fact] void should_leave_the_connection_as_found_after_checking_a_table() => _afterTable.ShouldEqual(ConnectionState.Open);
    [Fact] void should_leave_the_connection_as_found_after_reading_columns() => _afterColumns.ShouldEqual(ConnectionState.Open);
}
