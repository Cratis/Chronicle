// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

public class and_the_event_has_a_single_write_of_a_removal_on_sqlite : and_the_event_has_a_single_write_of_a_removal<SqlSinkHarness>
{
    protected override SqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Interceptors = [capture] };

    protected override int RecordedCommands => 2;

    protected override string RecordedFingerprint => "751279d92b554b02f28665f20a4755ef8f840b6d9e10e675262bfd7e94089d1d";
}
