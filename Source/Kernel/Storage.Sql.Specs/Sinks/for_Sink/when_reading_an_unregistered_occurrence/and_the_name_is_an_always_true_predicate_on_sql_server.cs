// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_reading_an_unregistered_occurrence;

[Collection(SqlServerCollection.Name)]
public class and_the_name_is_an_always_true_predicate_on_sql_server(SqlServerFixture fixture) : and_the_name_is_an_always_true_predicate<SqlServerSinkHarness>
{
    protected override SqlServerSinkHarness CreateSqlHarness() => new() { Fixture = fixture };

    [Fact] void should_not_fail_the_read() => PageReceived().ShouldBeTrue();
    [Fact] void should_return_no_instances() => InstanceCount().ShouldEqual(0);
    [Fact] void should_not_create_a_table() => _created.ShouldBeFalse();
}
