// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Schema;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_validating_cycle_receipts;

/// <summary>
/// Verify against a receipt table with one more index. A unique or filtered index can make a data violation look like
/// a receipt collision, so only a plain index is accepted.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_the_receipt_table_has_an_extra_index<THarness> : given.a_receipt_cycle<THarness>
    where THarness : ISqlSinkHarness
{
    Exception? _plain;
    Exception? _unique;
    Exception? _filtered;
    ulong _sequenceNumber = 1;

    async Task Establish() => await Seed();

    async Task Because()
    {
        _plain = await VerifyWith("CREATE INDEX ix_receipt_extra ON {0} ({1})");
        _unique = await VerifyWith("CREATE UNIQUE INDEX ux_receipt_extra ON {0} ({1})");
        _filtered = await VerifyWith("CREATE INDEX ix_receipt_extra ON {0} ({1}) WHERE {1} > 0");
    }

    [Fact] public void should_accept_a_plain_extra_index() => _plain.ShouldBeNull();
    [Fact] public void should_reject_a_unique_extra_index() => _unique.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] public void should_name_the_unique_index() => _unique!.Message.ShouldContain("ux_receipt_extra");
    [Fact] public void should_reject_a_filtered_extra_index() => _filtered.ShouldBeOfExactType<SchemaNotCurrent>();

    async Task<Exception?> VerifyWith(string statement)
    {
        await Provision(AnotherSink(), $"provisioning-{_sequenceNumber}", ++_sequenceNumber);
        await Execute(string.Format(statement, Quote(ReceiptTable), Quote("EventSequenceNumber")));
        _verify = true;
        var exception = await Attempt(AnotherSink(), $"verified-{_sequenceNumber}", ++_sequenceNumber);
        _verify = false;
        await DropReceiptTable();
        return exception;
    }
}
