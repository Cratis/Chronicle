// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_idempotently;

public class with_an_effect_receipt : given.a_receipted_append
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => ((IEventSequenceWithEffectReceipts)_eventSequence).AppendManyWithEffectReceipt(
        [new("source", "created") { IdempotencyKey = new("k") }], _scopes, _receipt, null));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<IdempotentAppendsNotSupported>();
    [Fact] void should_not_send_anything() => _request.ShouldBeNull();
}
