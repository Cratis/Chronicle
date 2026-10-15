// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_reading_occurrence_receipts;

public class with_multiple_occurrences_in_view : given.a_receipt_storage
{
    IEnumerable<EffectReceipt> _receipts;
    async Task Establish()
    {
        await Recreate().AppendManyWithEffectReceipt([], Receipt(null));
        await Recreate().AppendManyWithEffectReceipt([], Receipt(null) with { Id = "second-effect", Lease = new("second-occurrence", 2) });
        foreach (var index in Enumerable.Range(0, 100))
        {
            await Recreate().AppendManyWithEffectReceipt([], Receipt(null) with { Id = $"unrelated-effect-{index}", Lease = new($"unrelated-occurrence-{index}", 1) });
        }
    }

    async Task Because() => _receipts = await Recreate().GetEffectReceiptsForOccurrences([new("occurrence"), new("second-occurrence"), new("occurrence"), new("missing")]);
    [Fact] void should_include_exactly_the_effects_in_view() => _receipts.Select(receipt => receipt.Id).ShouldContainOnly(_receiptId, "second-effect");
    [Fact] void should_preserve_each_occurrence_identity() => _receipts.Select(receipt => receipt.Lease.OccurrenceId.Value).ShouldContainOnly("occurrence", "second-occurrence");
}
