// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_with_an_effect_receipt;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_batch_has_no_events(ReplicaSetMongoDBFixture fixture) : given.a_receipt_storage(fixture)
{
    EffectReceiptWriteResult _result;
    EffectReceipt _stored;
    ulong _count;
    async Task Because()
    {
        _result = await _storage.AppendManyWithEffectReceipt([], Receipt(null));
        _stored = (await Recreate().GetEffectReceipt(_receiptId))!;
        _count = (await _storage.GetCount()).Value;
    }
    [Fact] void should_commit_the_receipt() => _result.ShouldEqual(EffectReceiptWriteResult.Written);
    [Fact] void should_record_no_events() => _stored.Outcome.ShouldEqual(EffectOutcome.NoEvents);
    [Fact] void should_preserve_the_response_verbatim() => _stored.Response.ShouldEqual("{\"winner\":1}");
    [Fact] void should_not_append_an_event() => _count.ShouldEqual(0UL);
}
