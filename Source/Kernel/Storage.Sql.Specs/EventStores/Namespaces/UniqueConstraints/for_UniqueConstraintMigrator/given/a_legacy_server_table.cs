// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintMigrator.given;

#pragma warning disable EF1002 // Only fixture-owned identifiers, delimited by the provider; row values are parameters.

public abstract class a_legacy_server_table : Specification
{
    protected string _tableName = $"claims_{Guid.NewGuid():N}";
    protected UniqueConstraintDbContext _context;
    protected DbContextOptions<UniqueConstraintDbContext> _options;
    protected UniqueConstraintMigrator _migrator;
    protected migration_warnings _logger;
    protected string _connectionString;
    protected string _table;
    protected string _value;
    protected Func<Task> _dropDatabase;

    protected async Task CreateLegacyTable(string connectionString, bool sqlServer, Func<Task> dropDatabase)
    {
        _connectionString = connectionString;
        _dropDatabase = dropDatabase;
        _logger = new();
        _migrator = new(
            new TableMigrator<UniqueConstraintDbContext>(Substitute.For<ILogger<TableMigrator<UniqueConstraintDbContext>>>()),
            new TableMigrator<UniqueConstraintValuesDbContext>(Substitute.For<ILogger<TableMigrator<UniqueConstraintValuesDbContext>>>()),
            _logger);
        var builder = new DbContextOptionsBuilder<UniqueConstraintDbContext>();
        if (sqlServer)
        {
            builder.UseSqlServer(connectionString).WithoutServiceProviderCaching();
        }
        else
        {
            builder = new(SpecDbContextOptions.Kernel<UniqueConstraintDbContext>(connectionString));
        }

        _options = builder.Options;
        _context = new(_options, _tableName, _migrator);
        await _context.Database.OpenConnectionAsync();
        var helper = _context.GetService<ISqlGenerationHelper>();
        _table = helper.DelimitIdentifier(_tableName);
        _value = helper.DelimitIdentifier("Value");
        var source = helper.DelimitIdentifier("EventSourceId");
        var sequence = helper.DelimitIdentifier("SequenceNumber");
        var index = helper.DelimitIdentifier($"IX_{_tableName}_Value");
        var sourceType = sqlServer ? "NVARCHAR(200)" : "TEXT";
        var valueType = sqlServer ? "NVARCHAR(MAX)" : "TEXT";
        await _context.Database.ExecuteSqlRawAsync($"CREATE TABLE {_table} ({source} {sourceType} NOT NULL PRIMARY KEY, {_value} {valueType} NOT NULL, {sequence} DECIMAL(20,0) NOT NULL)");
        if (!sqlServer)
        {
            await _context.Database.ExecuteSqlRawAsync($"CREATE INDEX {index} ON {_table} ({_value})");
        }

        await _context.Database.CloseConnectionAsync();
    }

    protected Task AddClaim(string source, string value) => _context.Database.ExecuteSqlRawAsync(
        $"INSERT INTO {_table} ({_context.GetService<ISqlGenerationHelper>().DelimitIdentifier("EventSourceId")}, {_value}, {_context.GetService<ISqlGenerationHelper>().DelimitIdentifier("SequenceNumber")}) VALUES ({{0}}, {{1}}, 42)", source, value);

    protected async Task<long> Scalar(string query)
    {
        await _context.Database.OpenConnectionAsync();
        try
        {
            await using var command = _context.Database.GetDbConnection().CreateCommand();
#pragma warning disable CA2100 // Fixture-owned table identifiers are delimited by the provider.
            command.CommandText = query;
#pragma warning restore CA2100
            var result = await command.ExecuteScalarAsync();
            return result is null or DBNull ? -1 : Convert.ToInt64(result);
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }

    protected Task<long> IndexIsUnique() => Scalar(_context.Database.IsSqlServer()
        ? $"SELECT CAST(is_unique AS INT) FROM sys.indexes WHERE object_id = OBJECT_ID('{_tableName}') AND name = 'IX_{_tableName}_Value'"
        : $"SELECT CASE WHEN i.indisunique THEN 1 ELSE 0 END FROM pg_index i JOIN pg_class idx ON idx.oid = i.indexrelid WHERE idx.relname = CAST('IX_{_tableName}_Value' AS name)");

    protected Task<long> ColumnLength() => Scalar($"SELECT CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{_tableName}' AND COLUMN_NAME = 'Value'");

    protected string[] Warnings() => _logger.Warnings.ToArray();

    async Task Destroy()
    {
        await _context.DisposeAsync();
        await _dropDatabase();
    }
}
