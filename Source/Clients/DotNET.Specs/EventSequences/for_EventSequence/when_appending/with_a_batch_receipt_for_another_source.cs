// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_a_batch_receipt_for_another_source : given.an_acknowledged_append
{
    void Establish() => _batchResponse.Receipts.Last().EventSourceId = "another-source";

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.AppendMany([new(_source, "first"), new(_source, "second")]));

    [Fact] void should_fail_explicitly() => _error.ShouldBeOfExactType<InvalidAppendReceipt>();
    [Fact] void should_not_notify_partial_success() => _notifications.ShouldBeEmpty();
}
