// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_removing_a_child;

[Collection(SqlServerCollection.Name)]
public class and_the_key_is_composite_on_sql_server(SqlServerFixture fixture) : given.a_sink_with_composite_children<SqlServerSinkHarness>
{
    protected override SqlServerSinkHarness CreateHarness() => new() { Fixture = fixture };

    Task Because() => RemoveCompositeChild();

    [Fact] void should_keep_only_the_siblings() => _remaining.Select(child => child["name"]).ShouldEqual(new[] { "tenant/other", "other/target" });
    [Fact] void should_find_the_parent_with_the_target() => _holdingTarget.Count().ShouldEqual(1);
    [Fact] void should_not_find_a_parent_with_a_different_key() => _holdingMissing.ShouldBeEmpty();
}
