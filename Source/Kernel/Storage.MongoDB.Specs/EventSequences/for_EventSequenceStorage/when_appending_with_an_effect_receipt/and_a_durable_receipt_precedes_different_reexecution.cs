// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_with_an_effect_receipt;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_a_durable_receipt_precedes_different_reexecution(ReplicaSetMongoDBFixture fixture) : given.a_receipt_storage(fixture)
{
    EffectReceiptWriteResult _result;
    EffectReceipt _stored;
    ulong _count;
    async Task Establish() => await _storage.AppendManyWithEffectReceipt([], Receipt(null));
    async Task Because()
    {
        _result = await Recreate().AppendManyWithEffectReceipt([EventAt(0)], Receipt(0) with { Response = "null" });
        _stored = (await _storage.GetEffectReceipt(_receiptId))!;
        _count = (await _storage.GetCount()).Value;
    }
    [Fact] void should_keep_the_first_outcome() => _result.ShouldEqual(EffectReceiptWriteResult.AlreadyRecorded);
    [Fact] void should_keep_the_first_response() => _stored.Response.ShouldEqual("{\"winner\":1}");
    [Fact] void should_append_nothing() => _count.ShouldEqual(0UL);
}
