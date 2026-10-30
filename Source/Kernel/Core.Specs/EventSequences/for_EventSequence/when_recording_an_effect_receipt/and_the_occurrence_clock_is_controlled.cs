// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_recording_an_effect_receipt;

public class and_the_occurrence_clock_is_controlled : given.an_event_sequence_with_effect_receipts
{
    protected override TimeProvider EventTimeProvider => new given.EventClock();

    async Task Because() => await _eventSequence.RecordEffectReceipt(_receipt, EffectOutcome.NoEvents, []);

    [Fact] async Task should_use_the_clock_for_the_receipt() => (await _receiptStorage.GetEffectReceipt(ReceiptId)).RecordedAt.ShouldEqual(given.EventClock.Now);
}
