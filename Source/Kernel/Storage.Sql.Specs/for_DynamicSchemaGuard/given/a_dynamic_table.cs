// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.for_DynamicSchemaGuard.given;

public class a_dynamic_table : for_Database.given.a_sqlite_database
{
    ILogger<DynamicSchemaGuard> _logger;
    protected DbContext _context;
    protected TableMigrator<DbContext> _migrator;
    protected string _table = $"test_{Guid.NewGuid():N}";
    protected int _creates;

    async Task Establish()
    {
        _logger = Substitute.For<ILogger<DynamicSchemaGuard>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var guard = new DynamicSchemaGuard(_logger);
        var options = new DbContextOptionsBuilder().UseSqlite(_connectionString).WithoutServiceProviderCaching();
        ((IDbContextOptionsBuilderInfrastructure)options).AddOrUpdateExtension(new SchemaModeOptionsExtension(SchemaMode.Verify));
        _context = DynamicSchemaGuard.Attach(new DbContext(options.Options), guard);
        await _context.Database.OpenConnectionAsync();
        _migrator = new TableMigrator<DbContext>(Substitute.For<ILogger<TableMigrator<DbContext>>>());
    }

    protected Task Ensure() => _migrator.EnsureTableMigrated(_table, _context, async (context, table) =>
    {
        _creates++;
        var migration = new MigrationBuilder(context.Database.ProviderName);
        migration.CreateTable(table, columns => new { Id = columns.Column<long>(nullable: false) }, constraints: constraint => constraint.PrimaryKey($"PK_{table}", column => column.Id));
        await _migrator.ExecuteMigrationOperations(context, migration);
    });

    protected int Logs(LogLevel level) => _logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log) && Equals(call.GetArguments()[0], level));

    async Task Destroy() => await _context.DisposeAsync();
}
