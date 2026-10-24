// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ensuring_indexes;

[Collection(PostgreSqlCollection.Name)]
public class and_reconciliation_follows_an_active_builder_on_postgresql(PostgreSqlFixture fixture) : when_reconciliation_follows_an_active_builder
{
    protected override ISqlSinkHarness CreateHarness(OnlineIndexGate gate) => new PostgreSqlSinkHarness { Interceptors = [gate], Fixture = fixture };
}
