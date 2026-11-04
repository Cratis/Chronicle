// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// The receipt table disappears after the sink created it: the next cycle creates it again instead of failing.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_the_receipt_table_was_dropped<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    long _tables;

    async Task Establish()
    {
        await Apply("target", 1, Set(("joinId", "J")));
        await Apply("first", 2, KeyedFromJoin("joinId", "J", ("x", 1)), Set(("group", "g")));
        await Execute($"DROP TABLE {Quote(ReceiptTable)}");
    }

    async Task Because()
    {
        try
        {
            await Apply("second", 3, KeyedFromJoin("joinId", "J", ("x", 2)), Set(("group", "g")));
        }
        catch (Exception exception)
        {
            _error = exception;
        }

        _tables = await Scalar(IsSqlite
            ? $"SELECT CAST(COUNT(*) AS bigint) AS \"Value\" FROM sqlite_master WHERE name = '{ReceiptTable}'"
            : $"SELECT CAST(COUNT(*) AS bigint) AS \"Value\" FROM information_schema.tables WHERE table_name = '{ReceiptTable}'");
    }

    [Fact] public void should_succeed() => _error.ShouldBeNull();
    [Fact] public void should_recreate_the_table() => _tables.ShouldEqual(1);
}
