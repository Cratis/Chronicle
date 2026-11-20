// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_reporting_dormancy_metrics;

public class and_an_event_wakes_an_observer : given.a_queue_reporting_its_metrics
{
    async Task Because()
    {
        await _queue.Enqueue([EventAt(1)]);
        await _queue.AwaitQueueDepletion(periodNum: 500);
    }

    [Fact] void should_count_the_wake_by_event() => Recorded(WellKnownMetricNames.ObserverDormantWakes, "event").ShouldEqual(1);
    [Fact] void should_not_count_a_safety_net_wake() => Recorded(WellKnownMetricNames.ObserverDormantWakes, "safety_net").ShouldEqual(0);
    [Fact] void should_not_count_a_delayed_wake_while_places_are_free() => Recorded(WellKnownMetricNames.ObserverDormantWakesDelayed).ShouldEqual(0);
}
