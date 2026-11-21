// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_reporting_dormancy_metrics;

public class and_an_observer_is_released : given.a_queue_reporting_its_metrics
{
    async Task Establish() => await _queue.Subscribe(KeyOf("released"), [_eventType]);

    async Task Because() => await _queue.TryMarkDormant(KeyOf("released"));

    [Fact] void should_count_the_release() => Recorded(WellKnownMetricNames.ObserverDormantReleases).ShouldEqual(1);
}
