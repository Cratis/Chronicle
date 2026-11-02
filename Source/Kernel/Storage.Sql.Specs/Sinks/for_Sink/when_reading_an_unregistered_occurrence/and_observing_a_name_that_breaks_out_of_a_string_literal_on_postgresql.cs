// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_reading_an_unregistered_occurrence;

[Collection(PostgreSqlCollection.Name)]
public class and_observing_a_name_that_breaks_out_of_a_string_literal_on_postgresql(PostgreSqlFixture fixture) : and_observing_a_name_that_breaks_out_of_a_string_literal<PostgreSqlSinkHarness>
{
    protected override PostgreSqlSinkHarness CreateSqlHarness() => new() { Fixture = fixture };

    [Fact] async Task should_keep_the_canary() => (await CanaryExists()).ShouldBeTrue();
    [Fact] void should_emit_no_instances() => InstanceCount().ShouldEqual(0);
    [Fact] void should_not_create_a_table() => _created.ShouldBeFalse();
}
