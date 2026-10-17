// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_effect_receipt;

public class with_named_tags : given.a_receipted_append
{
    async Task Because() => _result = await ((IEventSequenceWithEffectReceipts)_eventSequence).AppendManyWithEffectReceipt(
        [new("source", "created") { NamedTags = [new("category", "receipt")] }], _scopes, _receipt, null);

    [Fact] void should_use_the_named_tag_transport() => _taggedRequest.Events.Single().NamedTags.Single().Value.ShouldEqual("receipt");
    [Fact] void should_carry_the_receipt_in_the_same_request() => _taggedRequest.EffectReceipt.ShouldEqual(_receipt);
    [Fact] void should_return_the_recorded_response() => _result.EffectReceipt.Receipt.Response.ToJsonString().ShouldEqual("\"winner\"");
    [Fact] void should_report_the_writing_epoch_not_the_current_attempt() => _result.EffectReceipt.Receipt.WrittenBy.Epoch.ShouldEqual(1L);
    [Fact] void should_notify_only_once() => _notifications.ShouldEqual(1);
    [Fact] void should_not_use_the_untagged_transport() => _request.ShouldBeNull();
}
