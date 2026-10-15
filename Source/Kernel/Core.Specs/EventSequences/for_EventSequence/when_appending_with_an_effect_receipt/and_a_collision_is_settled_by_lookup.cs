// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_effect_receipt;

public class and_a_collision_is_settled_by_lookup : given.an_event_sequence_with_effect_receipts
{
    int _lookups;

    async Task Establish()
    {
        await AppendEffect();
        _collision = EffectReceiptWriteResult.AlreadyRecorded;
        _receiptStorage.GetEffectReceipt(Arg.Any<string>()).Returns(call => ++_lookups == 1
            ? Task.FromResult<EffectReceipt?>(null)
            : _durableStorage.GetEffectReceipt(call.Arg<string>()));
        _receipt = _receipt with { Response = "{\"answer\":100}" };
    }

    async Task Because() => _result = await AppendEffect();

    [Fact] void should_return_the_winning_effect() => _result.EffectAlreadyRecorded.ShouldBeTrue();
    [Fact] void should_return_the_winning_response() => _result.EffectReceipt!.Response.ShouldEqual("{\"answer\":42}");
    [Fact] void should_not_repeat_events() => _durableStorage.Events.Count.ShouldEqual(1);
}
