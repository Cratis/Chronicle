// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

public class and_the_event_has_a_single_write_of_a_single_join_on_sqlite : and_the_event_has_a_single_write_of_a_single_join<SqlSinkHarness>
{
    protected override SqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Interceptors = [capture] };

    protected override int RecordedCommands => 2;

    protected override string RecordedFingerprint => "4d885c3e7de11cfd465c689a2b1291153aecbf7bbcc4483317cc08c53b09eca4";
}
