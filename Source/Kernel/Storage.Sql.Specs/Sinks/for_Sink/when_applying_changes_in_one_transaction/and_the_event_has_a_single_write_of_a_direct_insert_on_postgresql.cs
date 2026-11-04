// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

[Collection(PostgreSqlCollection.Name)]
public class and_the_event_has_a_single_write_of_a_direct_insert_on_postgresql(PostgreSqlFixture fixture) : and_the_event_has_a_single_write_of_a_direct_insert<PostgreSqlSinkHarness>
{
    protected override PostgreSqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Fixture = fixture, Interceptors = [capture] };

    protected override int RecordedCommands => 6;

    protected override string RecordedFingerprint => "a620666fe1640afa00f3df32793a0dc865c6266efb6f0a2deda13b182fd3e4e6";
}
