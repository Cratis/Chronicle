// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_connections;

[Collection(PostgreSqlCollection.Name)]
public class and_a_schema_change_holds_the_lock(PostgreSqlFixture fixture) : given.a_postgresql_database_with_connection_resolution(fixture)
{
    string _role;
    string _holder;

    async Task Establish()
    {
        _role = $"spec_{Guid.NewGuid():N}";
        await Execute(ConnectionString, $"CREATE ROLE \"{_role}\" LOGIN SUPERUSER PASSWORD 'resolved-password'");
        Resolver.Answer = _ => new ResolvedConnection(Identity: new(_role, "resolved-password"));
    }

    async Task Because()
    {
        var database = ResolvingDatabase(_ => _.Identity.UseCustom<given.RecordingConnectionResolver>());
        await using var scope = await database.Namespace(Store, Namespace);
        await DatabaseSchemaLock.Run(scope.DbContext, async () => _holder = await scope.DbContext.Database.SqlQueryRaw<string>(
            "SELECT activity.usename::text AS \"Value\" FROM pg_locks locks JOIN pg_stat_activity activity ON activity.pid = locks.pid " +
            "WHERE locks.locktype = 'advisory' AND locks.granted AND locks.database = (SELECT oid FROM pg_database WHERE datname = current_database())").SingleAsync());
    }

    [Fact] void should_hold_it_as_the_resolved_user() => _holder.ShouldEqual(_role);
}
