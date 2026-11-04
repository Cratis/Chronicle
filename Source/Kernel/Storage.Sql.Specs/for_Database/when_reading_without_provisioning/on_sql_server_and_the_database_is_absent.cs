// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_reading_without_provisioning;

[Collection(SqlServerCollection.Name)]
public class on_sql_server_and_the_database_is_absent(SqlServerFixture fixture) : given.a_sql_server_database(fixture)
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
        _connection = ConnectionFor(await ReadModelDatabaseName());
        _name = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_connection).InitialCatalog;
        await Fixture.DropDatabase(_connection);
        _databasesBefore = await PhysicalDatabases();
    }

    async Task Because()
    {
        _scope = await Database.ExistingReadModelTable(Store, Namespace, "primary", OccurrenceColumns);
        _databasesAfter = await PhysicalDatabases();
        _boundAfter = await ReadModelDatabaseName();

        // Put the database back so the specification can clean up after itself.
        await Execute(ConnectionString, $"CREATE DATABASE [{_name}]");
    }

    [Fact] void should_have_dropped_the_database_in_the_setup() => _databasesBefore.ShouldNotContain(_name);
    [Fact] void should_report_the_table_as_absent() => _scope.ShouldBeNull();
    [Fact] void should_not_recreate_the_database() => _databasesAfter.ShouldNotContain(_name);
    [Fact] void should_keep_the_binding_as_it_was() => _boundAfter.ShouldEqual(_name);
}
