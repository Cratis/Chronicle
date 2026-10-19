// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintMigrator.given;

public class a_legacy_sqlite_table : Specification
{
    protected const string TableName = "legacy_claims";
    protected SqliteConnection _connection;
    protected UniqueConstraintDbContext _context;
    protected UniqueConstraintMigrator _migrator;
    protected migration_warnings _logger;

    async Task Establish()
    {
        _connection = new SqliteConnection($"DataSource=migration-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await _connection.OpenAsync();
        _logger = new();
        _migrator = new(
            new TableMigrator<UniqueConstraintDbContext>(Substitute.For<ILogger<TableMigrator<UniqueConstraintDbContext>>>()),
            new TableMigrator<UniqueConstraintValuesDbContext>(Substitute.For<ILogger<TableMigrator<UniqueConstraintValuesDbContext>>>()),
            _logger);
        _context = new(new DbContextOptionsBuilder<UniqueConstraintDbContext>().UseSqlite(_connection.ConnectionString).WithoutServiceProviderCaching().Options, TableName, _migrator);
        await _context.Database.ExecuteSqlRawAsync("CREATE TABLE legacy_claims (EventSourceId TEXT PRIMARY KEY, Value TEXT NOT NULL, SequenceNumber INTEGER NOT NULL); CREATE INDEX IX_legacy_claims_Value ON legacy_claims (Value)");
    }

    protected async Task<bool> IndexIsUnique()
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT \"unique\" FROM pragma_index_list('legacy_claims') WHERE name = 'IX_legacy_claims_Value'";
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    async Task Destroy()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
