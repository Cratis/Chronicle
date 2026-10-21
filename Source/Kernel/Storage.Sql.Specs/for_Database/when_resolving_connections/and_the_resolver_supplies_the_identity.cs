// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_connections;

[Collection(PostgreSqlCollection.Name)]
public class and_the_resolver_supplies_the_identity(PostgreSqlFixture fixture) : given.a_postgresql_database_with_connection_resolution(fixture)
{
    string _role;
    string _connectedAs;

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
        _connectedAs = await scope.DbContext.Database.SqlQueryRaw<string>("SELECT current_user AS \"Value\"").SingleAsync();
    }

    [Fact] void should_connect_as_the_resolved_user() => _connectedAs.ShouldEqual(_role);
}
