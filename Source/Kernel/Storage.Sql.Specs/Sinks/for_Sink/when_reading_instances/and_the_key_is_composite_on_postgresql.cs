// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_reading_instances;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_reading_instances;

[Collection(PostgreSqlCollection.Name)]
public class and_the_key_is_composite_on_postgresql(PostgreSqlFixture fixture) : Contract.and_the_key_is_composite<PostgreSqlSinkHarness>
{
    protected override PostgreSqlSinkHarness CreateHarness() => new() { Fixture = fixture };
}
