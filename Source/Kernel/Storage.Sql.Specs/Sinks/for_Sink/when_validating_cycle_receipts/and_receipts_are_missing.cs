// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Schema;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_validating_cycle_receipts;

/// <summary>
/// Verify against a database whose receipt table was never provisioned, with credentials that could create it.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_receipts_are_missing<THarness> : given.a_receipt_cycle<THarness>
    where THarness : ISqlSinkHarness
{
    protected Exception? _first;
    protected Exception? _second;
    protected bool _tableCreated;
    protected bool _indexCreated;

    async Task Establish()
    {
        await Seed();
        _verify = true;
        _capture.Clear();
    }

    async Task Because()
    {
        var sink = AnotherSink();
        _first = await Attempt(sink, "source", 2);
        _second = await Attempt(sink, "source", 3);
        _tableCreated = await ReceiptTableExists();
        _indexCreated = await ReceiptIndexExists();
    }

    [Fact] public void should_report_the_schema_as_not_current() => _first.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] public void should_report_it_again_for_the_next_cycle() => _second.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] public void should_not_create_the_receipt_table() => _tableCreated.ShouldBeFalse();
    [Fact] public void should_not_create_the_receipt_index() => _indexCreated.ShouldBeFalse();
    [Fact] public void should_issue_no_receipt_ddl() => ReceiptDdl.ShouldBeEmpty();
    [Fact] public void should_not_write_to_the_read_model() => ReadModelWrites.ShouldBeEmpty();
    [Fact] public void should_not_start_a_transaction() => _capture.Started.ShouldEqual(0);
}
