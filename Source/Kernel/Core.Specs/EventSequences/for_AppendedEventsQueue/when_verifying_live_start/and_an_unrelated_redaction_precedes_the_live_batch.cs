// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_verifying_live_start;

public class and_an_unrelated_redaction_precedes_the_live_batch : given.a_queue_verifying_live_gaps
{
    async Task Establish()
    {
        _history = [RedactionAt(102, new EventTypeId("unsubscribed"))];
        await _queue.Subscribe(_observerKey, [_eventType, new(GlobalEventTypes.Redaction, EventTypeGeneration.First)], LiveFilters);
    }

    Task Because() => Deliver(MatchingAt(105));

    [Fact] void should_deliver_the_live_batch() => _handledEvents.ShouldEqual(1);
    [Fact] void should_apply_the_original_type_check_after_the_storage_filter() => _observer.DidNotReceive().CatchUp();
}
