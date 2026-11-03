// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_validating_cycle_receipts;

/// <summary>
/// A Verify cycle whose commit never reaches the server is retried by probing its receipt, without revalidating the schema.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_a_cycle_is_retried<THarness> : given.a_receipt_cycle<THarness>
    where THarness : ISqlSinkHarness
{
    Exception? _error;
    IDictionary<string, object?>? _row;
    bool _table;
    bool _index;

    async Task Establish()
    {
        await Seed();
        await Provision(_sink, "provisioning", 2);
        _verify = true;
        var sink = AnotherSink();
        await MultiWrite(sink, "admitted", 3);
        _capture.Clear();
        _capture.FailNextCommitBeforeSending();
        _retrying = sink;
    }

    ISink? _retrying;

    async Task Because()
    {
        _error = await Attempt(_retrying!, "retried", 4);
        _row = await Row("retried");
        _table = await ReceiptTableExists();
        _index = await ReceiptIndexExists();
    }

    [Fact] public void should_succeed() => _error.ShouldBeNull();
    [Fact] public void should_apply_the_cycle() => _row.ShouldNotBeNull();
    [Fact] public void should_retry_once() => _capture.Started.ShouldEqual(2);
    [Fact] public void should_probe_the_receipt_first_in_the_retry() => ReceiptProbes.Count().ShouldEqual(1);
    [Fact] public void should_not_revalidate_the_schema() => CatalogReads.ShouldBeEmpty();
    [Fact] public void should_keep_the_receipt_table_and_index() => (_table && _index).ShouldBeTrue();
    [Fact] public void should_issue_no_receipt_ddl() => ReceiptDdl.ShouldBeEmpty();
}
