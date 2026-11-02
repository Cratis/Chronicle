// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_reading_an_unregistered_occurrence;

[Collection(SqlServerCollection.Name)]
public class and_the_occurrence_table_exists_on_sql_server(SqlServerFixture fixture) : and_the_occurrence_table_exists<SqlServerSinkHarness>
{
    protected override SqlServerSinkHarness CreateSqlHarness() => new() { Fixture = fixture };

    [Fact] void should_not_fail_the_read() => _error.ShouldBeNull();
    [Fact] void should_return_the_instance_of_the_occurrence() => InstanceCount().ShouldEqual(1);
}
