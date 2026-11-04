// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_reading_without_provisioning;

[Collection(SqlServerCollection.Name)]
public class on_sql_server_and_the_table_is_absent(SqlServerFixture fixture) : given.a_sql_server_database(fixture)
{
    string _connection;
    DbContextScope<ReadModelDbContext>? _scope;
    string _primaryBefore;
    string _ghostAfter;
    string _primaryAfter;

    async Task Establish()
    {
        await ProvisionPrimaryTable();
        _connection = ConnectionFor(await ReadModelDatabaseName());
        _primaryBefore = await TableShape(_connection, "primary");
    }

    async Task Because()
    {
        _scope = await Database.ExistingReadModelTable(Store, Namespace, "ghost-20240101000000-deadbeef", OccurrenceColumns);
        _ghostAfter = await TableShape(_connection, "ghost-20240101000000-deadbeef");
        _primaryAfter = await TableShape(_connection, "primary");
    }

    [Fact] void should_have_provisioned_the_primary_table_in_the_setup() => _primaryBefore.ShouldNotBeEmpty();
    [Fact] void should_report_the_table_as_absent() => _scope.ShouldBeNull();
    [Fact] void should_not_create_the_table() => _ghostAfter.ShouldBeEmpty();
    [Fact] void should_leave_the_primary_table_as_it_was() => _primaryAfter.ShouldEqual(_primaryBefore);
}
