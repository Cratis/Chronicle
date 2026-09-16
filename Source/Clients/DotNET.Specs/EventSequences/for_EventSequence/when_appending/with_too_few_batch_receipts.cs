// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_too_few_batch_receipts : given.an_acknowledged_append
{
    void Establish() => _batchResponse.Receipts = _batchResponse.Receipts.Take(1).ToArray();

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.AppendMany(_source, ["first", "second"]));

    [Fact] void should_fail_explicitly() => _error.ShouldBeOfExactType<InvalidAppendReceipt>();
    [Fact] void should_not_notify_partial_success() => _notifications.ShouldBeEmpty();
}
