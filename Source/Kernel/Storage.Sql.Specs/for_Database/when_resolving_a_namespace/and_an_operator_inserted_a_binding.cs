// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_a_namespace;

[Collection(PostgreSqlCollection.Name)]
public class and_an_operator_inserted_a_binding(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    string _target;
    string _actual;

    async Task Establish()
    {
        _target = new Npgsql.NpgsqlConnectionStringBuilder(ConnectionString).Database + "_operator";
        await CreateLegacyDatabase(_target);
        await using var cluster = await Database.Cluster();
        cluster.DbContext.DatabaseNames.Add(new DatabaseNameBinding { Kind = DatabaseKind.Namespace, EventStore = Store, Namespace = Namespace, PhysicalName = _target });
        await cluster.DbContext.SaveChangesAsync();
    }

    async Task Because()
    {
        await using var scope = await Database.Namespace(Store, Namespace);
        _actual = scope.DbContext.Database.GetDbConnection().Database;
    }

    [Fact] void should_honor_the_operator_selected_database() => _actual.ShouldEqual(_target);
}
