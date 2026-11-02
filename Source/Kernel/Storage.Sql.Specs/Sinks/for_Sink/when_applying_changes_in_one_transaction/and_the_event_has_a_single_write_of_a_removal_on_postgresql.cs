// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

[Collection(PostgreSqlCollection.Name)]
public class and_the_event_has_a_single_write_of_a_removal_on_postgresql(PostgreSqlFixture fixture) : and_the_event_has_a_single_write_of_a_removal<PostgreSqlSinkHarness>
{
    protected override PostgreSqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Fixture = fixture, Interceptors = [capture] };

    protected override int RecordedCommands => 2;

    protected override string RecordedFingerprint => "f5b4aa088e96d5c599a8b3382e7aea516c2e02998b105946d25ff600c3b79d64";
}
