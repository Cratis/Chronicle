// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.for_Database.given;

public class a_sqlite_read_model_table : a_sqlite_database
{
    readonly ProjectedColumn[] _columns = [new("Id", typeof(string), true, false, false, false)];

    async Task Establish()
    {
        var migrator = new ReadModelMigrator(
            new TableMigrator<ReadModelDbContext>(NullLogger<TableMigrator<ReadModelDbContext>>.Instance),
            NullLogger<ReadModelMigrator>.Instance);
        _readModelMigrator.EnsureTableMigrated(Arg.Any<string>(), Arg.Any<ReadModelDbContext>())
            .Returns(call => migrator.EnsureTableMigrated(call.Arg<string>(), call.Arg<ReadModelDbContext>()));

        await using var scope = await ReadModelTable();
    }

    protected Task<DbContextScope<ReadModelDbContext>> ReadModelTable() =>
        _database.ReadModelTable("store", "namespace", "replay-model", _columns);
}
