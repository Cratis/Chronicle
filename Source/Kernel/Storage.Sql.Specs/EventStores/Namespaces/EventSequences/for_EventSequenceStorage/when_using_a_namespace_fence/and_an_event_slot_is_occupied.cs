// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_using_a_namespace_fence;

public class and_an_event_slot_is_occupied : given.a_fenced_storage
{
    EffectReceiptWriteResult _receipt;
    DuplicateEventSequenceNumber _duplicate;

    async Task Establish() => (await _fenced.Append(_context, _event)).IsSuccess.ShouldBeTrue();

    async Task Because()
    {
        _receipt = await _fenced.AppendManyWithEffectReceipt(_context, [_event], Receipt(0));
        _duplicate = (await _fenced.Append(_context, _event)).AsT1;
    }

    [Fact] void should_report_the_duplicate_for_the_receipt() => _receipt.ShouldEqual(EffectReceiptWriteResult.DuplicateEventSequenceNumber);
    [Fact] void should_report_the_next_slot() => _duplicate.NextAvailableSequenceNumber.ShouldEqual(new EventSequenceNumber(1));
    [Fact] async Task should_roll_back_the_receipt() => (await _fenced.GetEffectReceipt(_context, _receiptId)).ShouldBeNull();
    [Fact] async Task should_not_append_again() => (await _storage.GetCount()).Value.ShouldEqual(1UL);
}
