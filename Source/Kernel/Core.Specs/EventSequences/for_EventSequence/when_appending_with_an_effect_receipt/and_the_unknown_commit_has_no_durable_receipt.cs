// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_effect_receipt;

public class and_the_unknown_commit_has_no_durable_receipt : given.an_event_sequence_with_effect_receipts
{
    void Establish() => _failWithoutCommit = true;
    async Task Because() => _result = await AppendEffect();

    [Fact] void should_report_a_retryable_unsettled_effect() => _result.Errors.ShouldContainOnly(AppendError.EffectReceiptUnsettled);
    [Fact] void should_not_claim_the_effect_was_recorded() => _result.EffectAlreadyRecorded.ShouldBeFalse();
    [Fact] void should_return_no_receipt() => _result.EffectReceipt.ShouldBeNull();
    [Fact] async Task should_store_no_receipt() => (await _receiptStorage.GetEffectReceipt(ReceiptId)).ShouldBeNull();
    [Fact] void should_append_no_events() => _durableStorage.Events.ShouldBeEmpty();
    [Fact] void should_not_update_constraint_indexes() => _constraintIndexSequenceNumbers.ShouldBeEmpty();
    [Fact] async Task should_not_dispatch_events() => await _appendedEventsQueues.DidNotReceive().Enqueue(Arg.Any<IEnumerable<AppendedEvent>>());
}
