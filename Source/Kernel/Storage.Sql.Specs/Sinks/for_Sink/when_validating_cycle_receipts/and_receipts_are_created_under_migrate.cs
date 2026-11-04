// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_validating_cycle_receipts;

/// <summary>
/// The positive control: under Migrate the same cycle creates the receipt table and its CreatedAt index, and the capture sees that DDL.
/// Without it, the "no receipt DDL" checks under Verify could never fail.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_receipts_are_created_under_migrate<THarness> : given.a_receipt_cycle<THarness>
    where THarness : ISqlSinkHarness
{
    Exception? _error;
    bool _table;
    bool _index;

    async Task Establish()
    {
        await Seed();
        _verify = false;
        _capture.Clear();
    }

    async Task Because()
    {
        _error = await Attempt(AnotherSink(), "created", 2);
        _table = await ReceiptTableExists();
        _index = await ReceiptIndexExists();
    }

    [Fact] public void should_succeed() => _error.ShouldBeNull();
    [Fact] public void should_create_the_table() => _table.ShouldBeTrue();
    [Fact] public void should_create_the_index() => _index.ShouldBeTrue();
    [Fact] public void should_capture_the_create_table_ddl() => ReceiptDdl.ShouldContain($"CREATE TABLE {ReceiptTable}");
    [Fact] public void should_capture_the_create_index_ddl() => ReceiptDdl.Any(operation => operation.StartsWith("CREATE INDEX", StringComparison.Ordinal)).ShouldBeTrue();
}
