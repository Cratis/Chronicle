// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Schema;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_validating_cycle_receipts;

/// <summary>
/// Verify against a provisioned receipt table whose CreatedAt index was dropped: the cycle fails closed and the index stays absent.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_the_created_at_index_is_missing<THarness> : given.a_receipt_cycle<THarness>
    where THarness : ISqlSinkHarness
{
    Exception? _first;
    Exception? _second;
    bool _table;
    bool _index;

    async Task Establish()
    {
        await Seed();
        await Provision(_sink, "provisioning", 2);
        await DropReceiptIndex();
        _verify = true;
        _capture.Clear();
    }

    async Task Because()
    {
        var sink = AnotherSink();
        _first = await Attempt(sink, "first", 3);
        _second = await Attempt(sink, "second", 4);
        _table = await ReceiptTableExists();
        _index = await ReceiptIndexExists();
    }

    [Fact] public void should_report_the_schema_as_not_current() => _first.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] public void should_name_the_index() => _first!.Message.ShouldContain(SinkCycleReceiptSchema.IndexName);
    [Fact] public void should_report_it_again_for_the_next_cycle() => _second.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] public void should_not_create_the_index() => _index.ShouldBeFalse();
    [Fact] public void should_keep_the_table() => _table.ShouldBeTrue();
    [Fact] public void should_issue_no_ddl() => DdlIn(_capture.Commands).ShouldBeEmpty();
    [Fact] public void should_reach_the_table_migrator_with_no_ddl() => ReceiptDdl.ShouldBeEmpty();
    [Fact] public void should_not_write_to_the_read_model() => ReadModelWrites.ShouldBeEmpty();
}
