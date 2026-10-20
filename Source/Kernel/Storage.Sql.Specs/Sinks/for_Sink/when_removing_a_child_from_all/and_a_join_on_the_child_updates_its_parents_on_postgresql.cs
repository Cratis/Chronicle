// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_removing_a_child_from_all;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_removing_a_child_from_all;

[Collection(PostgreSqlCollection.Name)]
public class and_a_join_on_the_child_updates_its_parents_on_postgresql(PostgreSqlFixture fixture) : Contract.and_a_join_on_the_child_updates_its_parents<PostgreSqlSinkHarness>
{
    protected override PostgreSqlSinkHarness CreateHarness() => new() { Fixture = fixture };
}
