// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_reading_without_provisioning;

[Collection(PostgreSqlCollection.Name)]
public class on_postgresql_and_the_database_is_absent(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    string _name;
    string _connection;
    DbContextScope<ReadModelDbContext>? _scope;
    string[] _databasesBefore;
    string[] _databasesAfter;
    string _boundAfter;

    async Task Establish()
    {
        await ProvisionPrimaryTable();
        _connection = await ReadModelConnection();
        _name = new Npgsql.NpgsqlConnectionStringBuilder(_connection).Database!;
        await Fixture.DropDatabase(_connection);

        // The drop evicts the server side of pooled connections; the pools must not hand them to the read.
        Npgsql.NpgsqlConnection.ClearAllPools();
        _databasesBefore = await PhysicalDatabases();
    }

    async Task Because()
    {
        _scope = await Database.ExistingReadModelTable(Store, Namespace, "primary", OccurrenceColumns);
        _databasesAfter = await PhysicalDatabases();
        _boundAfter = await Binding(DatabaseKind.ReadModels, Store, Namespace);
    }

    [Fact] void should_have_dropped_the_database_in_the_setup() => _databasesBefore.ShouldNotContain(_name);
    [Fact] void should_report_the_table_as_absent() => _scope.ShouldBeNull();
    [Fact] void should_not_recreate_the_database() => _databasesAfter.ShouldNotContain(_name);
    [Fact] void should_keep_the_binding_as_it_was() => _boundAfter.ShouldEqual(_name);
}
