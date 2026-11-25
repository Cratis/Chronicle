// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_verifying_live_start;

public class and_a_redaction_for_a_subscribed_type_is_missing : given.a_queue_verifying_live_gaps
{
    async Task Establish()
    {
        _history = [RedactionAt(102, _eventType.Id)];
        await _queue.Subscribe(_observerKey, [_eventType, new(GlobalEventTypes.Redaction, EventTypeGeneration.First)], LiveFilters);
    }

    Task Because() => Deliver(MatchingAt(105));

    [Fact] void should_not_deliver_past_the_redaction() => _handledEvents.ShouldEqual(0);
    [Fact] void should_request_catch_up() => _observer.Received(1).CatchUp();
    [Fact] void should_name_the_missing_redaction_sequence() => LiveStartGapWarnings.Single()["MissingEventSequenceNumber"].ShouldEqual((EventSequenceNumber)102UL);
    [Fact] void should_include_redactions_in_the_bounded_storage_filter() => _events.Received(1).GetRange(
        (EventSequenceNumber)102UL,
        (EventSequenceNumber)104UL,
        null,
        Arg.Is<IEnumerable<EventType>>(types => types.Any(type => type.Id == GlobalEventTypes.Redaction)),
        Arg.Any<IEnumerable<Tag>>(),
        null,
        null,
        Arg.Any<CancellationToken>());
}
