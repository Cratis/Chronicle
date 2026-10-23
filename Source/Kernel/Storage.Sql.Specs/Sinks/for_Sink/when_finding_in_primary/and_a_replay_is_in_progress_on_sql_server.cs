// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_finding_in_primary;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_finding_in_primary;

[Collection(SqlServerCollection.Name)]
public class and_a_replay_is_in_progress_on_sql_server(SqlServerFixture fixture) : Contract.and_a_replay_is_in_progress<SqlServerSinkHarness>
{
    protected override SqlServerSinkHarness CreateHarness() => new() { Fixture = fixture };
}
