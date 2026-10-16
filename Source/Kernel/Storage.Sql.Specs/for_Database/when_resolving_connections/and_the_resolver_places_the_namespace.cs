// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_connections;

[Collection(PostgreSqlCollection.Name)]
public class and_the_resolver_places_the_namespace(PostgreSqlFixture fixture) : given.a_postgresql_database_with_connection_resolution(fixture)
{
    string _placed;
    string _actual;
    bool _bound;

    void Establish()
    {
        _placed = $"{DatabaseName}_placed";
        AdditionalDatabases.Add(_placed);
        Resolver.Answer = request => request.Scope[StorageConnectionRequests.Kind] == "namespace"
            ? new ResolvedConnection(Database: _placed)
            : ResolvedConnection.Unchanged;
    }

    async Task Because()
    {
        var database = ResolvingDatabase(_ => _.Connection.UseCustom<given.RecordingConnectionResolver>());
        await using (var scope = await database.Namespace(Store, Namespace))
        {
            _actual = scope.DbContext.Database.GetDbConnection().Database;
        }

        await using var cluster = await Database.Cluster();
        _bound = await cluster.DbContext.DatabaseNames.AnyAsync(row => row.Kind == DatabaseKind.Namespace);
    }

    [Fact] void should_connect_to_the_placed_database() => _actual.ShouldEqual(_placed);
    [Fact] void should_not_bind_a_database_the_platform_placed() => _bound.ShouldBeFalse();
    [Fact] void should_resolve_the_namespace_for_its_event_store() => Resolver.Requests.ShouldContain(_ => _.Scope[StorageConnectionRequests.EventStore] == Store && _.Scope[StorageConnectionRequests.Namespace] == Namespace);
    [Fact] void should_resolve_for_runtime() => Resolver.Requests.ShouldContain(_ => _.Scope[StorageConnectionRequests.Kind] == "namespace" && _.Purpose == ConnectionPurpose.Runtime);
    [Fact] void should_resolve_for_schema_to_migrate() => Resolver.Requests.ShouldContain(_ => _.Scope[StorageConnectionRequests.Kind] == "namespace" && _.Purpose == ConnectionPurpose.Schema);
    [Fact] void should_resolve_for_postgresql() => Resolver.Requests.ShouldEachConformTo(_ => _.Storage == WellKnownConnectionStorage.PostgreSql);
}
