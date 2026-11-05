// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_finding_keys_holding_a_child;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_finding_keys_holding_a_child;

[Collection(PostgreSqlCollection.Name)]
public class and_the_collection_is_nested_on_postgresql(PostgreSqlFixture fixture) : Contract.and_the_collection_is_nested<PostgreSqlSinkHarness>
{
    protected override PostgreSqlSinkHarness CreateHarness() => new() { Fixture = fixture };
}
