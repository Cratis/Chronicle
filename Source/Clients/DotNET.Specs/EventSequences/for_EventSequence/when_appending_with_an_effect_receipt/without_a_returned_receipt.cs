// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_effect_receipt;

public class without_a_returned_receipt : given.a_receipted_append
{
    Exception _error;

    void Establish() => _response.EffectReceipt = null;

    async Task Because() => _error = await Catch.Exception(() => ((IEventSequenceWithEffectReceipts)_eventSequence).AppendManyWithEffectReceipt([new("source", "created")], _scopes, _receipt, null));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<EffectReceiptNotValid>();
    [Fact] void should_not_notify_as_success() => _notifications.ShouldEqual(0);
}
