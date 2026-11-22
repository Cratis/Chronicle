// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorageV2;

[Collection(SqlServerCollection.Name)]
public class when_running_the_sql_server_contract(SqlServerFixture fixture) : given.a_sql_v2_storage
{
    protected override bool OwnerSpacesEqual => true;

    protected override Task<string> CreateDatabase() => fixture.CreateDatabase();

    protected override DbContextOptionsBuilder Configure(DbContextOptionsBuilder builder, string connectionString) => builder.UseSqlServer(connectionString);

    protected override Task ReleaseDatabase() => fixture.DropDatabase(_connectionString);
}
