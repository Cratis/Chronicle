// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class without_an_acknowledged_receipt : given.an_acknowledged_append
{
    void Establish() => _response.Receipt = null;

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.Append(_source, "event"));

    [Fact] void should_fail_explicitly() => _error.ShouldBeOfExactType<InvalidAppendReceipt>();
    [Fact] void should_warn_that_the_append_may_be_persisted() => _error.Message.ShouldContain("acknowledged and may already be persisted");
    [Fact] void should_not_notify_a_guessed_success() => _notifications.ShouldBeEmpty();
}
