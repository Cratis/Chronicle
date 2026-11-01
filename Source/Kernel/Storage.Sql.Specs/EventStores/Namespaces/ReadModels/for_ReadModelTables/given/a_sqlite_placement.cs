// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTables.given;

public class a_sqlite_placement : Specification
{
    private protected readonly ProjectedColumn[] _columns = [new("id", typeof(string), true, false, false, false)];
    private protected readonly ReadModelTableHandle _live = ReadModelTableHandle.For("store", "default", "model", "orders", ReadModelTableRole.Live, "orders");
    private protected SinkReadModelTablePlacements _placements;
    private protected IReadModelTables _tables;
    SqliteConnection _connection;

    async Task Establish()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();
        var migrator = new ReadModelMigrator(
            new TableMigrator<ReadModelDbContext>(NullLogger<TableMigrator<ReadModelDbContext>>.Instance),
            NullLogger<ReadModelMigrator>.Instance);
        _placements = new(builder => builder.UseSqlite(_connection).AddConceptAsSupport(), _connection.ConnectionString, migrator);
        _tables = _placements.Tables;
    }

    void Destroy() => _connection.Dispose();
}
