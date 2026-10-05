// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_ending_a_replay;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

[Collection(SqlServerCollection.Name)]
public class and_a_second_replay_follows_the_first_on_sql_server(SqlServerFixture fixture) : Contract.and_a_second_replay_follows_the_first<SqlServerSinkHarness>
{
    protected override SqlServerSinkHarness CreateHarness() => new() { Fixture = fixture };
}
