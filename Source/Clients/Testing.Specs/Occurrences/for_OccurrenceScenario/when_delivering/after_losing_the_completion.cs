// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Occurrences.for_OccurrenceScenario.when_delivering;

public class after_losing_the_completion : given.a_scenario
{
    OccurrenceDeliveryOutcome _first;
    OccurrenceDeliveryOutcome _retry;
    int _count;
    long _epoch;

    async Task Because()
    {
        _first = await _scenario.DeliverWithoutCompleting();
        _scenario.Time.Advance(TimeSpan.FromSeconds(60));
        _retry = await _scenario.DeliverNext();
        _epoch = (await _scenario.StatusOf(_recorded.Id))!.Epoch;
        _count = (await _scenario.EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Count;
    }

    [Fact] void should_leave_the_first_lease_uncompleted() => _first.ShouldEqual(OccurrenceDeliveryOutcome.Delivered);
    [Fact] void should_reclaim_and_complete() => _retry.ShouldEqual(OccurrenceDeliveryOutcome.Completed);
    [Fact] void should_raise_the_epoch() => _epoch.ShouldEqual(2L);
    [Fact] void should_append_the_effect_only_once() => _count.ShouldEqual(1);
}
