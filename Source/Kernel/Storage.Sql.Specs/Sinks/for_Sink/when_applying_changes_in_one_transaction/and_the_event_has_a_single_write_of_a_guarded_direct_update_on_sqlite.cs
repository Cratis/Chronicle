// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

public class and_the_event_has_a_single_write_of_a_guarded_direct_update_on_sqlite : and_the_event_has_a_single_write_of_a_guarded_direct_update<SqlSinkHarness>
{
    protected override SqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Interceptors = [capture] };

    protected override int RecordedCommands => 2;

    protected override string RecordedFingerprint => "db8e74c02fe86fdb1aad9971395f5fb6a7b89fffba66a71d1055ebee1c1695ca";
}
