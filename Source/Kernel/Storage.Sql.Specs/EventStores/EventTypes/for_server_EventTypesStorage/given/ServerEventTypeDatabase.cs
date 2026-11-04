// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_server_EventTypesStorage.given;

public class ServerEventTypeDatabase(
    Func<Task<string>> create,
    Func<string, Task> drop,
    Action<DbContextOptionsBuilder<EventStoreDbContext>, string> configure)
{
    string _connectionString = string.Empty;

    public static ServerEventTypeDatabase PostgreSql(PostgreSqlFixture fixture) => new(fixture.CreateDatabase, fixture.DropDatabase, (builder, connection) => builder.UseNpgsql(connection));

    public static ServerEventTypeDatabase SqlServer(SqlServerFixture fixture) => new(fixture.CreateDatabase, fixture.DropDatabase, (builder, connection) => builder.UseSqlServer(connection));

    public async Task<DbContextOptions<EventStoreDbContext>> Create()
    {
        _connectionString = await create();
        var builder = new DbContextOptionsBuilder<EventStoreDbContext>();
        configure(builder, _connectionString);
        return builder.AddConceptAsSupport().WithoutServiceProviderCaching().Options;
    }

    public Task Drop() => drop(_connectionString);
}
