// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_reading_an_unregistered_occurrence;

[Collection(SqlServerCollection.Name)]
public class and_observing_a_name_that_breaks_out_of_a_string_literal_on_sql_server(SqlServerFixture fixture) : and_observing_a_name_that_breaks_out_of_a_string_literal<SqlServerSinkHarness>
{
    protected override SqlServerSinkHarness CreateSqlHarness() => new() { Fixture = fixture };

    [Fact] void should_receive_a_page() => PageReceived().ShouldBeTrue();
    [Fact] async Task should_keep_the_canary() => (await CanaryExists()).ShouldBeTrue();
    [Fact] void should_emit_no_instances() => InstanceCount().ShouldEqual(0);
    [Fact] void should_not_create_a_table() => _created.ShouldBeFalse();
}
