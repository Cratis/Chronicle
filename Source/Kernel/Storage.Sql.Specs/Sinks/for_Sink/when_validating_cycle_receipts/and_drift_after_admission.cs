// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Schema;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_validating_cycle_receipts;

/// <summary>
/// The receipt table is lost after a Verify sink admitted its schema: that cycle fails closed without DDL, and the next
/// cycle revalidates and reports the schema before its body runs.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_drift_after_admission<THarness> : given.a_receipt_cycle<THarness>
    where THarness : ISqlSinkHarness
{
    Exception? _drifted;
    Exception? _next;
    int _nextCatalogReads;
    int _nextWrites;
    bool _tableCreated;
    IDictionary<string, object?>? _driftedRow;

    async Task Establish()
    {
        await Seed();
        await Provision(_sink, "provisioning", 2);
        _verify = true;
    }

    async Task Because()
    {
        var sink = AnotherSink();
        await MultiWrite(sink, "admitted", 4);
        await DropReceiptTable();
        _capture.Clear();
        _drifted = await Attempt(sink, "drifted", 5);
        _driftedRow = await Row("drifted");
        _capture.Clear();
        _next = await Attempt(sink, "next", 6);
        _nextCatalogReads = CatalogReads.Count();
        _nextWrites = ReadModelWrites.Count();
        _tableCreated = await ReceiptTableExists();
    }

    [Fact] public void should_fail_the_cycle_after_admission() => _drifted.ShouldNotBeNull();
    [Fact] public void should_report_the_schema_for_the_cycle() => _drifted.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] public void should_roll_the_cycle_back() => _driftedRow.ShouldBeNull();
    [Fact] public void should_revalidate_in_the_next_cycle() => _nextCatalogReads.ShouldEqual(1);
    [Fact] public void should_report_the_schema_for_the_next_cycle() => _next.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] public void should_not_enter_the_body_of_the_next_cycle() => _nextWrites.ShouldEqual(0);
    [Fact] public void should_not_recreate_the_table() => _tableCreated.ShouldBeFalse();
    [Fact] public void should_issue_no_receipt_ddl() => ReceiptDdl.ShouldBeEmpty();
}
