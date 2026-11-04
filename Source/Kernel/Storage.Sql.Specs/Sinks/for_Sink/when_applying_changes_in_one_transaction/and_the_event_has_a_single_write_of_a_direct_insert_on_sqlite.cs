// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

public class and_the_event_has_a_single_write_of_a_direct_insert_on_sqlite : and_the_event_has_a_single_write_of_a_direct_insert<SqlSinkHarness>
{
    protected override SqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Interceptors = [capture] };

    protected override int RecordedCommands => 5;

    protected override string RecordedFingerprint => "e3c50beaf1ac2cd5b313c516b20d4e144aadfe83ca66f0e31d124462c14eeb98";
}
