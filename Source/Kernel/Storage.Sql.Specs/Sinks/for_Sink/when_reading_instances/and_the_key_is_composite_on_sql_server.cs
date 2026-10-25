// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_reading_instances;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_reading_instances;

[Collection(SqlServerCollection.Name)]
public class and_the_key_is_composite_on_sql_server(SqlServerFixture fixture) : Contract.and_the_key_is_composite<SqlServerSinkHarness>
{
    protected override SqlServerSinkHarness CreateHarness() => new() { Fixture = fixture };
}
