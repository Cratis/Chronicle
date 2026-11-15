// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_replacing_isolated;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_replacing_isolated;

[Collection(PostgreSqlCollection.Name)]
public class and_properties_are_omitted_on_postgresql(PostgreSqlFixture fixture) : Contract.and_properties_are_omitted<PostgreSqlSinkHarness>
{
    protected override PostgreSqlSinkHarness CreateHarness() => new() { Fixture = fixture };
}
