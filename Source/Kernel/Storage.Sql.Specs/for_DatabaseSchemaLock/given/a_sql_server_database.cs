// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseSchemaLock.given;

public class a_sql_server_database(SqlServerFixture fixture) : a_database
{
    internal override IDatabaseSchemaLock SchemaLock => SqlServerDatabaseSchemaLock.Instance;

    internal override DbConnection NewConnection() => new SqlConnection(ConnectionString);

    internal override DbContextOptionsBuilder<SchemaLockDbContext> Configure(DbContextOptionsBuilder<SchemaLockDbContext> builder) => builder.UseSqlServer(ConnectionString);

    internal override async Task CreateDatabase() => ConnectionString = await fixture.CreateDatabase();

    internal override Task DropDatabase() => fixture.DropDatabase(ConnectionString);
}
