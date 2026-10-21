// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_reading_occurrence_receipts;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class with_unrelated_effects(ReplicaSetMongoDBFixture fixture) : given.a_receipt_storage(fixture)
{
    IEnumerable<EffectReceipt> _receipts;
    async Task Establish()
    {
        await Recreate().AppendManyWithEffectReceipt([], Receipt(null));
        await Recreate().AppendManyWithEffectReceipt([], Receipt(null) with { Id = "second-effect" });
        foreach (var index in Enumerable.Range(0, 100))
        {
            await Recreate().AppendManyWithEffectReceipt([], Receipt(null) with { Id = $"other-effect-{index}", Lease = new($"other-occurrence-{index}", 1) });
        }
    }

    async Task Because() => _receipts = await Recreate().GetEffectReceipts(new("occurrence"));
    [Fact] void should_include_only_the_requested_occurrences_effects() => _receipts.Select(receipt => receipt.Id).ShouldContainOnly(_receiptId, "second-effect");
    [Fact] void should_preserve_the_winning_response() => _receipts.Select(receipt => receipt.Response).Distinct().ShouldContainOnly("{\"winner\":1}");
}
