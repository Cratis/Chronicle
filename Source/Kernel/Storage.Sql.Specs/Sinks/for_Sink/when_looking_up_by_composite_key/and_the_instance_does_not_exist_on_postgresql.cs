// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_looking_up_by_composite_key;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_looking_up_by_composite_key;

[Collection(PostgreSqlCollection.Name)]
public class and_the_instance_does_not_exist_on_postgresql(PostgreSqlFixture fixture) : Contract.and_the_instance_does_not_exist<PostgreSqlSinkHarness>
{
    protected override PostgreSqlSinkHarness CreateHarness() => new() { Fixture = fixture };
}
