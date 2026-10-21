// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_idempotently;

public class a_single_event : given.an_idempotent_append
{
    AppendResult _result;

    async Task Because() => _result = await _eventSequence.AppendOnce("source", "event", _key);

    [Fact] void should_send_the_key() => _singleRequest.IdempotencyKey.ShouldEqual("order-1");
    [Fact] void should_report_the_outcome() => _result.Outcome.ShouldEqual(AppendOutcome.Appended);
    [Fact] void should_notify_once() => _notifications.ShouldEqual(1);
    [Fact] void should_advertise_support() => _eventSequence.SupportsIdempotentAppends.ShouldBeTrue();
}
