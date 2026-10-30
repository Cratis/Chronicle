// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_effect_receipt;

public class and_the_occurrence_clock_is_controlled : given.an_event_sequence_with_effect_receipts
{
    protected override TimeProvider EventTimeProvider => new given.EventClock();

    async Task Because() => _result = await AppendEffect();

    [Fact] void should_use_the_clock_for_the_event() => _durableStorage.Events.Single().Context.Occurred.ShouldEqual(given.EventClock.Now);
    [Fact] async Task should_use_the_clock_for_the_receipt() => (await _receiptStorage.GetEffectReceipt(ReceiptId))!.RecordedAt.ShouldEqual(given.EventClock.Now);
}
