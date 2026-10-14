// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_with_an_effect_receipt;

public class and_two_receipts_race : given.a_receipt_storage
{
    EffectReceiptWriteResult[] _results;
    EffectReceipt _stored;
    ulong _count;
    async Task Because()
    {
        _results = await StorageRace.Run(() => _storage.AppendManyWithEffectReceipt([EventAt(0)], Receipt(0)), () => Recreate().AppendManyWithEffectReceipt([EventAt(1)], Receipt(1) with { Response = "{\"winner\":2}" }));
        _stored = (await _storage.GetEffectReceipt(_receiptId))!;
        _count = (await _storage.GetCount()).Value;
    }
    [Fact] void should_commit_one_receipt() => _results.Count(result => result == EffectReceiptWriteResult.Written).ShouldEqual(1);
    [Fact] void should_write_only_the_winners_events() => _count.ShouldEqual(1UL);
    [Fact] void should_preserve_the_winners_response() => _stored.Response.ShouldEqual(_stored.FirstSequenceNumber!.Value == 0 ? "{\"winner\":1}" : "{\"winner\":2}");
}
