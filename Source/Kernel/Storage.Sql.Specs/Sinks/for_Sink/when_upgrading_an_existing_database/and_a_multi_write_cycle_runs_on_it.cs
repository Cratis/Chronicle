// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_upgrading_an_existing_database;

/// <summary>
/// Single-write cycles seed the database, then a multi-write cycle opens it and creates the receipt table on first use.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_a_multi_write_cycle_runs_on_it<THarness> : given.an_existing_database<THarness>
    where THarness : ISqlSinkHarness
{
    long _tableAfterSeed;
    long _rowsAfterSeed;
    long _tableAfterUpgrade;
    long _rowsAfterUpgrade;
    long _receiptsAfterUpgrade;

    async Task Because()
    {
        if (Stage != "upgrade")
        {
            // Single-write cycles only: they must not need, or create, a receipt table.
            await Apply("target", 1, Set(("joinId", "J")));
            await Apply("row", 2, Set(("group", "first")));
            await Apply("row", 3, Set(("group", "second")));
            _tableAfterSeed = await ReceiptTables();
            _rowsAfterSeed = await Rows();
        }

        if (Stage != "seed")
        {
            // A multi-write cycle: a join and a direct change.
            await Apply("row", 10, KeyedFromJoin("joinId", "J", ("x", 7)), Set(("group", "third")));
            _tableAfterUpgrade = await ReceiptTables();
            _rowsAfterUpgrade = await Rows();
            _receiptsAfterUpgrade = await ReceiptCount();
        }
    }

    [Fact] public void should_not_have_a_receipt_table_after_single_write_cycles() => (Stage == "upgrade" ? 0 : _tableAfterSeed).ShouldEqual(0);
    [Fact] public void should_have_written_the_seed_rows() => (Stage == "upgrade" ? 2 : _rowsAfterSeed).ShouldEqual(2);
    [Fact] public void should_create_the_receipt_table_on_the_first_multi_write_cycle() => (Stage == "seed" ? 1 : _tableAfterUpgrade).ShouldEqual(1);
    [Fact] public void should_keep_the_existing_rows() => (Stage == "seed" ? 2 : _rowsAfterUpgrade).ShouldEqual(2);
    [Fact] public void should_leave_no_receipt_behind() => (Stage == "seed" ? 0 : _receiptsAfterUpgrade).ShouldEqual(0);
}
