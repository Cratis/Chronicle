// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.for_Database.given;

/// <summary>
/// The real <see cref="Database"/> over SQLite with the real read model migrator, where only the cluster file exists.
/// Anything that provisions leaves a file or a table behind, which the specifications observe.
/// </summary>
public class a_database_without_read_model_storage : a_sqlite_database
{
    protected readonly ProjectedColumn[] _columns = [new("Id", typeof(string), true, false, false, false), new("Value", typeof(int), false, false, false, false)];

    void Establish()
    {
        var migrator = new ReadModelMigrator(
            new TableMigrator<ReadModelDbContext>(NullLogger<TableMigrator<ReadModelDbContext>>.Instance),
            NullLogger<ReadModelMigrator>.Instance);
        _readModelMigrator.EnsureTableMigrated(Arg.Any<string>(), Arg.Any<ReadModelDbContext>())
            .Returns(call => migrator.EnsureTableMigrated(call.Arg<string>(), call.Arg<ReadModelDbContext>()));
        _readModelMigrator.ProbeExistingTable(Arg.Any<string>(), Arg.Any<ReadModelDbContext>())
            .Returns(call => migrator.ProbeExistingTable(call.Arg<string>(), call.Arg<ReadModelDbContext>()));
    }

    /// <summary>
    /// Gets the names of the files in the directory the databases live in.
    /// </summary>
    protected IReadOnlyList<string> FilesOnDisk => [.. Directory.GetFiles(_directory).Select(Path.GetFileName).Order()!];
}
