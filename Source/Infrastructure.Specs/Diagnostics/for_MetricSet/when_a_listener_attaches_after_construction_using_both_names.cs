// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_MetricSet;

public class when_a_listener_attaches_after_construction_using_both_names : given.a_metric_listener_attached_late
{
    void Because()
    {
        var metrics = new MetricSet(_meter, MetricNames.Both, []);
        var appended = metrics.Counter<int>(WellKnownMetricNames.EventSequenceAppendedEvents, WellKnownMetricNames.EventSequenceAppendedEventsUnit, "Number of events appended to the event sequence");
        AttachListener();
        appended.Record(3);
    }

    [Fact] void should_receive_one_measurement_per_name_set() => _measurements.Count.ShouldEqual(2);
    [Fact] void should_receive_the_legacy_name() => _measurements.Count(_ => _.Name == "chronicle-event-sequences-appended-events").ShouldEqual(1);
    [Fact] void should_receive_the_convention_name() => _measurements.Count(_ => _.Name == "cratis.chronicle.event_sequence.appended_events").ShouldEqual(1);
    [Fact] void should_receive_the_recorded_value_in_both() => _measurements.TrueForAll(_ => _.Value == 3).ShouldBeTrue();
}
