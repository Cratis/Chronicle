// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_validating_cycle_receipts;

/// <summary>
/// Verify against receipts provisioned by Migrate: one read-only validation per sink, then none while warm.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_receipts_are_provisioned<THarness> : given.a_receipt_cycle<THarness>
    where THarness : ISqlSinkHarness
{
    Exception? _first;
    Exception? _warm;
    int _firstCatalogReads;
    int _warmCatalogReads;
    int _warmReceiptCommands;
    IDictionary<string, object?>? _row;
    string[] _firstCommands = [];
    string[] _firstReceiptDdl = [];
    bool _table;
    bool _index;

    async Task Establish()
    {
        await Seed();
        await Provision(_sink, "provisioning", 2);
        _verify = true;
        _capture.Clear();
    }

    async Task Because()
    {
        var sink = AnotherSink();
        _first = await Attempt(sink, "first", 3);
        _firstCatalogReads = CatalogReads.Count();
        _firstCommands = _capture.Commands.ToArray();
        _firstReceiptDdl = ReceiptDdl.ToArray();
        _capture.Clear();
        _warm = await Attempt(sink, "warm", 4);
        _warmCatalogReads = CatalogReads.Count();
        _warmReceiptCommands = ReceiptCommands.Count(command => !command.Contains("INSERT", StringComparison.OrdinalIgnoreCase) && !command.Contains("DELETE", StringComparison.OrdinalIgnoreCase));
        _row = await Row("warm");
        _table = await ReceiptTableExists();
        _index = await ReceiptIndexExists();
    }

    [Fact] public void should_admit_the_first_cycle() => _first.ShouldBeNull();
    [Fact] public void should_admit_the_warm_cycle() => _warm.ShouldBeNull();
    [Fact] public void should_validate_once_read_only_before_the_first_cycle() => _firstCatalogReads.ShouldEqual(1);
    [Fact] public void should_issue_no_catalog_commands_in_a_warm_cycle() => _warmCatalogReads.ShouldEqual(0);
    [Fact] public void should_issue_no_receipt_validation_in_a_warm_cycle() => _warmReceiptCommands.ShouldEqual(0);
    [Fact] public void should_issue_no_ddl_in_the_first_cycle_commands() => DdlIn(_firstCommands).ShouldBeEmpty();
    [Fact] public void should_reach_the_table_migrator_with_no_receipt_ddl_in_the_first_cycle() => _firstReceiptDdl.ShouldBeEmpty();
    [Fact] public void should_issue_no_receipt_ddl() => ReceiptDdl.ShouldBeEmpty();
    [Fact] public void should_keep_the_receipt_table() => _table.ShouldBeTrue();
    [Fact] public void should_keep_the_created_at_index() => _index.ShouldBeTrue();
    [Fact] public void should_write_the_row() => _row.ShouldNotBeNull();
    [Fact] public async Task should_leave_no_receipt() => (await ReceiptCount()).ShouldEqual(0);
}
