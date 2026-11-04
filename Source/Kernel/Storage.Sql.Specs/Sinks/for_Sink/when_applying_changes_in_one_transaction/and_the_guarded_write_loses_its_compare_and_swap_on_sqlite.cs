// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

public class and_the_guarded_write_loses_its_compare_and_swap_on_sqlite : and_the_guarded_write_loses_its_compare_and_swap<SqlSinkHarness>
{
    protected override SqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Interceptors = [capture], ConfigureOptions = given.retrying_on_timeout.Configure };

    protected override bool CompetesOnTheCyclesConnection => true;
}
