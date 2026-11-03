// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_validating_cycle_receipts;

public class and_receipts_are_missing_on_sqlite : and_receipts_are_missing<SqlSinkHarness>
{
    protected override SqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Interceptors = [capture], ConfigureOptions = Configure };
}
