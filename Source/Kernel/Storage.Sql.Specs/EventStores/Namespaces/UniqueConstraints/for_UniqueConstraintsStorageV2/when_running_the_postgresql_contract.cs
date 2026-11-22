// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorageV2;

[Collection(PostgreSqlCollection.Name)]
public class when_running_the_postgresql_contract(PostgreSqlFixture fixture) : UniqueConstraintsStorageV2SqlContract
{
    protected override Task<string> CreateDatabase() => fixture.CreateDatabase();

    protected override DbContextOptionsBuilder Configure(DbContextOptionsBuilder builder, string connectionString) => builder.UseNpgsql(connectionString);

    protected override Task ReleaseDatabase() => fixture.DropDatabase(_connectionString);
}
