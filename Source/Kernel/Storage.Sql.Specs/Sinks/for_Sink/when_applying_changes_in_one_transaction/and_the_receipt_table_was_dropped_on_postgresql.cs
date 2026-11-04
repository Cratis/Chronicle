// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

[Collection(PostgreSqlCollection.Name)]
public class and_the_receipt_table_was_dropped_on_postgresql(PostgreSqlFixture fixture) : and_the_receipt_table_was_dropped<PostgreSqlSinkHarness>
{
    protected override PostgreSqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Fixture = fixture, Interceptors = [capture] };
}
