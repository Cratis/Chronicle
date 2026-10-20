// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseSchemaLock.given;

public class a_postgresql_database(PostgreSqlFixture fixture) : a_database
{
    internal override IDatabaseSchemaLock SchemaLock => PostgreSqlDatabaseSchemaLock.Instance;

    internal override DbConnection NewConnection() => new NpgsqlConnection(ConnectionString);

    internal override DbContextOptionsBuilder<SchemaLockDbContext> Configure(DbContextOptionsBuilder<SchemaLockDbContext> builder) => builder.UseNpgsql(ConnectionString);

    internal override async Task CreateDatabase() => ConnectionString = await fixture.CreateDatabase();

    internal override Task DropDatabase() => fixture.DropDatabase(ConnectionString);
}
