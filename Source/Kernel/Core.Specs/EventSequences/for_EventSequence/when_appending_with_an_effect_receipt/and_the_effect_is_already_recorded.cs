// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_effect_receipt;

public class and_the_effect_is_already_recorded : given.an_event_sequence_with_effect_receipts
{
    async Task Establish()
    {
        await AppendEffect();
        _receipt = _receipt with { Response = "not valid JSON" };
        _eventSequenceStorage.ClearReceivedCalls();
    }

    async Task Because() => _result = await AppendEffect(new[] { AnEvent }.Select<EventToAppend, EventToAppend>(_ => throw new given.SimulatedStateWriteError()));

    [Fact] void should_return_the_existing_effect() => _result.EffectAlreadyRecorded.ShouldBeTrue();
    [Fact] void should_return_the_winning_response() => _result.EffectReceipt!.Response.ShouldEqual("{\"answer\":42}");
    [Fact] void should_not_append_again() => _durableStorage.Events.Count.ShouldEqual(1);
    [Fact] void should_not_revalidate_the_response() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_call_atomic_storage_again() => _receiptStorage.DidNotReceiveWithAnyArgs().AppendManyWithEffectReceipt(default!, default!);
}
