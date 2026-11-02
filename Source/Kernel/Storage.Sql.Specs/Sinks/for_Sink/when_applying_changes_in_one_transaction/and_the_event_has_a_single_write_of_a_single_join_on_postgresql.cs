// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

[Collection(PostgreSqlCollection.Name)]
public class and_the_event_has_a_single_write_of_a_single_join_on_postgresql(PostgreSqlFixture fixture) : and_the_event_has_a_single_write_of_a_single_join<PostgreSqlSinkHarness>
{
    protected override PostgreSqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Fixture = fixture, Interceptors = [capture] };

    protected override int RecordedCommands => 0;

    protected override string RecordedFingerprint => "";
}
