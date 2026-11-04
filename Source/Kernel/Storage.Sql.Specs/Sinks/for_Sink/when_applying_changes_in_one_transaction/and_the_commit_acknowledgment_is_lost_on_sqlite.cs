// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

public class and_the_commit_acknowledgment_is_lost_on_sqlite : and_the_commit_acknowledgment_is_lost<SqlSinkHarness>
{
    protected override SqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Interceptors = [capture], ConfigureOptions = given.retrying_on_timeout.Configure };
}
