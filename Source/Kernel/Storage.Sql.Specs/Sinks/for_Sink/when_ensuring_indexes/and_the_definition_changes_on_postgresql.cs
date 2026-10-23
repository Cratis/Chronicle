// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ensuring_indexes;

[Collection(PostgreSqlCollection.Name)]
public class and_the_definition_changes_on_postgresql(PostgreSqlFixture fixture) : when_reconciling_a_changed_definition
{
    protected override ISqlSinkHarness CreateHarness(OnlineIndexGate gate) => new PostgreSqlSinkHarness { Fixture = fixture, Interceptors = [gate] };
}
