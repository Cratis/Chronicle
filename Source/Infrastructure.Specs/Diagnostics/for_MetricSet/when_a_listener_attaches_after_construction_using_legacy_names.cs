// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_MetricSet;

public class when_a_listener_attaches_after_construction_using_legacy_names : given.a_metric_listener_attached_late
{
    void Because()
    {
        var metrics = new MetricSet(_meter, MetricNames.Legacy, []);
        var appended = metrics.Counter<int>(WellKnownMetricNames.EventSequenceAppendedEvents, WellKnownMetricNames.EventSequenceAppendedEventsUnit, "Number of events appended to the event sequence");
        AttachListener();
        appended.Record(3);
    }

    [Fact] void should_receive_one_measurement() => _measurements.Count.ShouldEqual(1);
    [Fact] void should_receive_the_legacy_name() => _measurements.Single().Name.ShouldEqual("chronicle-event-sequences-appended-events");
    [Fact] void should_receive_the_recorded_value() => _measurements.Single().Value.ShouldEqual(3);
}
