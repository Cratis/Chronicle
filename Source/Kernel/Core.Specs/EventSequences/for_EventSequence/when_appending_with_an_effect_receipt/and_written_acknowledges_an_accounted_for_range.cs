// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_effect_receipt;

public class and_written_acknowledges_an_accounted_for_range : given.an_event_sequence_with_effect_receipts
{
    async Task Establish()
    {
        await AppendEffect();
        var original = _receipt;
        _receipt = _receipt with { EffectKey = "later" };
        await AppendEffect();
        _receipt = original;
        _receiptStorage.AppendManyWithEffectReceipt(Arg.Any<IEnumerable<EventToAppendToStorage>>(), Arg.Any<Storage.Occurrences.EffectReceipt>())
            .Returns(Task.FromResult(EffectReceiptWriteResult.Written));
        _appendedEventsQueues.ClearReceivedCalls();
    }

    async Task Because() => _result = await _eventSequence.AppendManyToStorage([ValidatedEvent()], CorrelationId.New(), [], [], _receipt);
    [Fact] void should_return_the_durable_outcome() => _result.EffectAlreadyRecorded.ShouldBeTrue();
    [Fact] void should_preserve_the_next_slot() => _stateStorage.State.SequenceNumber.Value.ShouldEqual(2UL);
    [Fact] void should_not_reapply_constraint_updates() => _constraintIndexSequenceNumbers.Count.ShouldEqual(2);
    [Fact] async Task should_not_dispatch_historical_events() => await _appendedEventsQueues.DidNotReceiveWithAnyArgs().Enqueue(default!);
}
