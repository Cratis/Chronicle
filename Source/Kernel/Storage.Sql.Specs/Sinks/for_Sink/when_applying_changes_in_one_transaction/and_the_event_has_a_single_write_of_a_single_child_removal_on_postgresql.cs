// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

[Collection(PostgreSqlCollection.Name)]
public class and_the_event_has_a_single_write_of_a_single_child_removal_on_postgresql(PostgreSqlFixture fixture) : and_the_event_has_a_single_write_of_a_single_child_removal<PostgreSqlSinkHarness>
{
    protected override PostgreSqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Fixture = fixture, Interceptors = [capture] };

    protected override int RecordedCommands => 2;

    protected override string RecordedFingerprint => "963dc8be6b64a08bfc68d53f5dd5aab5e8b7f34ad41875d0afdd0efa07dd881f";
}
