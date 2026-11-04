// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_reading_without_provisioning;

[Collection(PostgreSqlCollection.Name)]
public class on_postgresql_and_an_older_table_is_missing_columns_and_indexes(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    string _connection;
    string _before;
    string _after;
    DbContextScope<ReadModelDbContext>? _scope;

    async Task Establish()
    {
        await ProvisionPrimaryTable();
        _connection = await ReadModelConnection();
        await CreateOlderTable(_connection, "older");
        _before = await TableShape(_connection, "older");
    }

    async Task Because()
    {
        _scope = await Database.ExistingReadModelTable(Store, Namespace, "older", OccurrenceColumns);
        _after = await TableShape(_connection, "older");
    }

    [Fact] void should_have_created_the_older_table_without_the_value_column_in_the_setup() => (_before.Contains("__initialized", StringComparison.Ordinal) && !_before.Contains("Value", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_the_table_as_absent() => _scope.ShouldBeNull();
    [Fact] void should_not_add_columns_or_indexes() => _after.ShouldEqual(_before);
}
