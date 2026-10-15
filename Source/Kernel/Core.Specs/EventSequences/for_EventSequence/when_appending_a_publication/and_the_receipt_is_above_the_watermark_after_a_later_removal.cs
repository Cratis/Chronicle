// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_a_publication;

public class and_the_receipt_is_above_the_watermark_after_a_later_removal : given.a_repairable_publication_sequence
{
    async Task Establish()
    {
        await Seed(0);
        await Seed(1, type: new("Removed", EventTypeGeneration.First));
    }

    async Task Because() => _result = await Publish();

    [Fact] void should_succeed() => _result.Errors.ShouldBeEmpty();
    [Fact] async Task should_leave_the_later_removal_in_place() => (await _claims.GetFor(_eventSourceId, _uniqueDefinition)).ShouldBeEmpty();
    [Fact] void should_never_write_the_retry_claim() => _claimWrites.ShouldEqual(0);
}
