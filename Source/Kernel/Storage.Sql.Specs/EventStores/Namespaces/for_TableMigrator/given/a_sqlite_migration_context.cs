// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_TableMigrator.given;

public class a_sqlite_migration_context : Specification
{
    private protected readonly ProjectedColumn[] _columns = [new("id", typeof(string), true, false, false, false)];
    private protected TableMigrator<ReadModelDbContext> _migrator;
    private protected ReadModelMigrator _readModelMigrator;
    private protected ReadModelDbContext _context;
    private protected SqliteConnection _connection;
    private protected string _file;

    void Establish()
    {
        _file = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        _connection = new SqliteConnection($"Data Source={_file}");
        _migrator = new(NullLogger<TableMigrator<ReadModelDbContext>>.Instance);
        _readModelMigrator = new(_migrator, NullLogger<ReadModelMigrator>.Instance);
        var options = new DbContextOptionsBuilder<ReadModelDbContext>().UseSqlite(_connection).AddConceptAsSupport().Options;
        _context = new ReadModelDbContext(options, "orders", _columns, _readModelMigrator);
    }

    void Destroy()
    {
        _context.Dispose();
        _connection.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_file);
    }

    private protected static MigrationBuilder CreateTable()
    {
        var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
        builder.Sql("CREATE TABLE probe (id TEXT NOT NULL PRIMARY KEY)");
        return builder;
    }

    private protected Task CreateOrders(ReadModelDbContext context, string _) =>
        context.Database.ExecuteSqlRawAsync("CREATE TABLE \"orders\" (\"id\" TEXT NOT NULL PRIMARY KEY)");
}
