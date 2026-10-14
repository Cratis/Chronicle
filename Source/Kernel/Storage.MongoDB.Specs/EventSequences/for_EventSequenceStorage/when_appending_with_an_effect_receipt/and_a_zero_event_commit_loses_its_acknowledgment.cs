// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_with_an_effect_receipt;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_a_zero_event_commit_loses_its_acknowledgment(ReplicaSetMongoDBFixture fixture) : given.a_receipt_storage(fixture)
{
    Exception _error;
    EffectReceipt _stored;
    ulong _count;
    async Task Because()
    {
        var decorated = new given.CommitThenFailEffectReceiptStorage(_storage);
        _error = await Catch.Exception(() => decorated.AppendManyWithEffectReceipt([], Receipt(null)));
        _stored = (await Recreate().GetEffectReceipt(_receiptId))!;
        _count = (await _storage.GetCount()).Value;
    }
    [Fact] void should_expose_the_lost_acknowledgment() => _error.ShouldBeOfExactType<given.AcknowledgmentLost>();
    [Fact] void should_keep_the_no_event_receipt() => _stored.Outcome.ShouldEqual(EffectOutcome.NoEvents);
    [Fact] void should_keep_the_sequence_empty() => _count.ShouldEqual(0UL);
}
