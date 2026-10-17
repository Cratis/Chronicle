// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Occurrences.for_OccurrenceScenario.when_delivering;

public class after_the_append_reply_is_dropped : given.a_scenario
{
    OccurrenceDeliveryOutcome _first;
    OccurrenceDeliveryOutcome _retry;
    int _count;

    void Establish() => _scenario.Replies.DropNext(DroppedReply.Append);

    async Task Because()
    {
        _first = await _scenario.DeliverNext();
        _scenario.Time.Advance(TimeSpan.FromSeconds(1));
        _retry = await _scenario.DeliverNext();
        _count = (await _scenario.EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Count;
    }

    [Fact] void should_record_the_unknown_handler_outcome_as_a_failure() => _first.ShouldEqual(OccurrenceDeliveryOutcome.Failed);
    [Fact] void should_reconcile_on_retry() => _retry.ShouldEqual(OccurrenceDeliveryOutcome.Completed);
    [Fact] void should_keep_one_set_of_events() => _count.ShouldEqual(1);
    [Fact] void should_drop_exactly_one_reply() => _scenario.Replies.Dropped.ShouldEqual(1);
}
