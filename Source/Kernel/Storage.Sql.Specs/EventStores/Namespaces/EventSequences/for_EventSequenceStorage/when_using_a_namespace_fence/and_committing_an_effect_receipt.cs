// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_using_a_namespace_fence;

public class and_committing_an_effect_receipt : given.a_fenced_storage
{
    EffectReceiptWriteResult _written;
    EffectReceiptWriteResult _replayed;
    EffectReceipt _receipt;
    EffectReceipt[] _receipts;

    async Task Because()
    {
        _written = await _fenced.AppendManyWithEffectReceipt(_context, [_event], Receipt(0));
        _replayed = await _fenced.AppendManyWithEffectReceipt(_context, [], Receipt(null));
        _receipt = (await _fenced.GetEffectReceipt(_context, _receiptId))!;
        _receipts = (await _fenced.GetEffectReceipts(_context, _receipt.Lease.OccurrenceId)).ToArray();
    }

    [Fact] void should_write_the_event_and_receipt() => _written.ShouldEqual(EffectReceiptWriteResult.Written);
    [Fact] void should_settle_reexecution() => _replayed.ShouldEqual(EffectReceiptWriteResult.AlreadyRecorded);
    [Fact] void should_preserve_the_first_outcome() => _receipt.Outcome.ShouldEqual(EffectOutcome.Appended);
    [Fact]
    void should_find_the_receipt_by_occurrence()
    {
        var receipt = _receipts.Single();
        receipt.Reasons.SequenceEqual(_receipt.Reasons).ShouldBeTrue();
        (receipt with { Reasons = _receipt.Reasons }).ShouldEqual(_receipt);
    }

    [Fact] async Task should_append_only_once() => (await _storage.GetCount()).Value.ShouldEqual(1UL);
}
