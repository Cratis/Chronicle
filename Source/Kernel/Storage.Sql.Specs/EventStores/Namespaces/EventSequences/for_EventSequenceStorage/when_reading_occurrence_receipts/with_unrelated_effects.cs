// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading_occurrence_receipts;

public class with_unrelated_effects : given.a_receipt_storage
{
    IEnumerable<EffectReceipt> _receipts;
    async Task Establish()
    {
        await Recreate().AppendManyWithEffectReceipt([], Receipt(null));
        await Recreate().AppendManyWithEffectReceipt([], Receipt(null) with { Id = "other-effect", Lease = new("other-occurrence", 1) });
    }

    async Task Because() => _receipts = await Recreate().GetEffectReceipts(new("occurrence"));
    [Fact] void should_include_only_the_requested_occurrences_effects() => _receipts.Select(receipt => receipt.Id).ShouldContainOnly(_receiptId);
    [Fact] void should_preserve_the_winning_response() => _receipts.Single().Response.ShouldEqual("{\"winner\":1}");
}
