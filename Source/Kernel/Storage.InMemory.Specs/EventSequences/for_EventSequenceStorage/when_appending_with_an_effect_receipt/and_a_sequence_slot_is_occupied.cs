// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_with_an_effect_receipt;

public class and_a_sequence_slot_is_occupied : given.a_receipt_storage
{
    EffectReceiptWriteResult _result;
    EffectReceipt? _stored;
    ulong _count;
    async Task Establish() => await _storage.AppendMany([EventAt(0)]);
    async Task Because()
    {
        _result = await _storage.AppendManyWithEffectReceipt([EventAt(1), EventAt(0)], Receipt(0) with { LastSequenceNumber = new EventSequenceNumber(1) });
        _stored = await _storage.GetEffectReceipt(_receiptId);
        _count = (await _storage.GetCount()).Value;
    }
    [Fact] void should_report_a_sequence_collision() => _result.ShouldEqual(EffectReceiptWriteResult.DuplicateEventSequenceNumber);
    [Fact] void should_write_no_receipt() => _stored.ShouldBeNull();
    [Fact] void should_not_write_the_unoccupied_slot() => _count.ShouldEqual(1UL);
}
