// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_reporting_dormancy_metrics;

public class and_the_safety_net_finds_an_observer_up_to_date : given.a_queue_reporting_its_metrics
{
    void Establish()
    {
        SetCursor(_observerKey.ObserverId, 11);
        _tails[_eventType.Id] = 10;
    }

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(SafetyNetSeconds));
        await _queue.RunDormantSafetyNet();
    }

    [Fact] void should_count_the_check_as_up_to_date() => Recorded(WellKnownMetricNames.ObserverDormantSafetyNetChecks, "up_to_date").ShouldEqual(1);
    [Fact] void should_not_count_a_wake() => Recorded(WellKnownMetricNames.ObserverDormantWakes).ShouldEqual(0);
}
