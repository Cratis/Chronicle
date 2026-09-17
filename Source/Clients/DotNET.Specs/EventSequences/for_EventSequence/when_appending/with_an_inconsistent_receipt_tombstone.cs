// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_an_inconsistent_receipt_tombstone : given.an_acknowledged_append
{
    void Establish() => _response.Receipt.EventType.Tombstone = true;

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.Append(_source, "event"));

    [Fact] void should_fail_explicitly() => _error.ShouldBeOfExactType<InvalidAppendReceipt>();
    [Fact] void should_not_notify_success() => _notifications.ShouldBeEmpty();
}
