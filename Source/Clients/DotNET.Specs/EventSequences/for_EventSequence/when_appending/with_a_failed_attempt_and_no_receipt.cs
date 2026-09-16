// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_a_failed_attempt_and_no_receipt : given.an_acknowledged_append
{
    AppendResult _result;

    void Establish()
    {
        _response.Errors = ["rejected"];
        _response.Receipt = null;
    }

    async Task Because() => _result = await _eventSequence.Append(_source, "event");

    [Fact] void should_return_the_failure() => _result.HasErrors.ShouldBeTrue();
    [Fact] void should_have_no_receipt() => _result.Receipt.ShouldBeNull();
    [Fact] void should_preserve_failed_attempt_notifications() => _notifications.Single().Result.ShouldEqual(_result);
    [Fact] void should_not_claim_a_persisted_sequence() => _notifications.Single().Event.Context.SequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_resolve_an_omitted_source_type() => _notifications.Single().Event.Context.EventSourceType.ShouldEqual(EventSourceType.Unspecified);
    [Fact] void should_not_invent_an_occurred_time() => _notifications.Single().Event.Context.Occurred.ShouldEqual(DateTimeOffset.MinValue);
}
