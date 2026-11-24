// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_namespace_bound;

public class appending_with_an_effect_receipt : given.a_namespace_bound_append
{
    async Task Because() => _result = await ((IEventSequenceWithEffectReceipts)_eventSequence).AppendManyWithEffectReceipt(
        [new("source", "event")], _scopes, _receipt, null);

    [Fact] void should_keep_the_receipt_on_the_protected_request() => _boundBatch.EffectReceipt.ShouldEqual(_receipt);
    [Fact] void should_send_the_captured_identity() => _boundBatch.ExpectedIncarnation.ShouldEqual(_binding.Incarnation.Value);
    [Fact] void should_return_the_recorded_response() => _result.EffectReceipt.Receipt.Response.ToJsonString().ShouldEqual("\"winner\"");
    [Fact] void should_not_downgrade() => ShouldNotUseLegacyAppendRoutes();
}
