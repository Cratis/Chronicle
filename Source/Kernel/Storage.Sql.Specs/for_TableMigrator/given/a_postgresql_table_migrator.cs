// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.for_DatabaseSchemaLock.given;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.for_TableMigrator.given;

public class a_postgresql_table_migrator(PostgreSqlFixture fixture) : a_postgresql_database(fixture)
{
    internal const string Table = "spec_table";
    internal readonly TableMigrator<SchemaLockDbContext> Migrator = new(NullLogger<TableMigrator<SchemaLockDbContext>>.Instance);

    internal static Task CreateTable(DbContext context) => context.Database.ExecuteSqlRawAsync($"CREATE TABLE \"{Table}\" (\"Id\" integer PRIMARY KEY)");

    internal async Task<bool> TableExists()
    {
        await using var context = new SchemaLockDbContext(Configure(new DbContextOptionsBuilder<SchemaLockDbContext>()).Options);
        return await context.Database.SqlQueryRaw<int>($"SELECT COUNT(*)::int AS \"Value\" FROM information_schema.tables WHERE table_name = '{Table}'").SingleAsync() > 0;
    }
}
