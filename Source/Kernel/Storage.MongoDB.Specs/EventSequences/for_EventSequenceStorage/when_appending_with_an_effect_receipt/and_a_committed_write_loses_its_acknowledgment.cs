// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_with_an_effect_receipt;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_a_committed_write_loses_its_acknowledgment(ReplicaSetMongoDBFixture fixture) : given.a_receipt_storage(fixture)
{
    Exception _error;
    EffectReceipt _stored;
    EffectReceiptWriteResult _retry;
    ulong _count;
    async Task Because()
    {
        var decorated = new given.CommitThenFailEffectReceiptStorage(_storage);
        _error = await Catch.Exception(() => decorated.AppendManyWithEffectReceipt([EventAt(0)], Receipt(0)));
        _stored = (await _storage.GetEffectReceipt(_receiptId))!;
        _retry = await Recreate().AppendManyWithEffectReceipt([EventAt(1)], Receipt(1));
        _count = (await _storage.GetCount()).Value;
    }
    [Fact] void should_expose_the_lost_acknowledgment() => _error.ShouldBeOfExactType<given.AcknowledgmentLost>();
    [Fact] void should_keep_the_receipt_with_its_events() => _stored.FirstSequenceNumber!.Value.ShouldEqual(0UL);
    [Fact] void should_settle_the_retry_without_writing() => _retry.ShouldEqual(EffectReceiptWriteResult.AlreadyRecorded);
    [Fact] void should_keep_exactly_one_event() => _count.ShouldEqual(1UL);
}
