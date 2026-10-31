// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_a_publication;

public class and_the_receipt_is_below_the_watermark : given.a_repairable_publication_sequence
{
    protected override EventSequenceNumber? Watermark => 2;
    Task Establish() => Seed(0);
    async Task Because() => _result = await Publish();

    [Fact] void should_succeed() => _result.Errors.ShouldBeEmpty();
    [Fact] void should_not_mutate_a_claim() => _claimWrites.ShouldEqual(0);
    [Fact] void should_not_read_a_repair_range() => _rangeReads.ShouldEqual(0);
}
