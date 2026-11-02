// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_reading_an_unregistered_occurrence;

[Collection(SqlServerCollection.Name)]
public class and_the_table_does_not_exist_on_sql_server(SqlServerFixture fixture) : and_the_table_does_not_exist<SqlServerSinkHarness>
{
    protected override SqlServerSinkHarness CreateSqlHarness() => new() { Fixture = fixture };

    [Fact] void should_not_create_the_table() => _created.ShouldBeFalse();
    [Fact] void should_return_no_instances() => InstanceCount().ShouldEqual(0);
}
