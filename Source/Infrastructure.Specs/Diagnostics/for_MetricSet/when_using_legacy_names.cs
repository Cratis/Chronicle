// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_MetricSet;

public class when_using_legacy_names : given.a_metric_listener
{
    int _initialMeasurements;

    void Because()
    {
        var metrics = new MetricSet(_meter, MetricNames.Legacy, [new(WellKnownTelemetryAttributes.EventStoreNamespace, "tenant")]);
        var appended = metrics.Counter<int>(WellKnownMetricNames.EventSequenceAppendedEvents, "{event}", "convention description", WellKnownTelemetryAttributes.EventTypeId);
        var lag = metrics.Gauge<long>(WellKnownMetricNames.ConstraintIndexLag, "{event}", "convention description");
        var wait = metrics.Histogram<double>(WellKnownMetricNames.SqlSchemaLockWaitDuration, "s", "convention description");
        _initialMeasurements = _measurements.Count;
        appended.Record(3, "Event");
        lag.Record(7);
        wait.Record(0.5);
    }

    [Fact] void should_create_only_the_three_selected_instruments() => _instruments.Count.ShouldEqual(3);
    [Fact] void should_not_record_initial_zeros() => _initialMeasurements.ShouldEqual(0);
    [Fact] void should_publish_each_measurement_once() => _measurements.Count.ShouldEqual(3);
    [Fact] void should_use_the_main_append_name() => _measurements.Single(_ => _.Value == 3).Name.ShouldEqual("chronicle-event-sequences-appended-events");
    [Fact] void should_preserve_the_main_append_description() => _instruments.Single(_ => _.Name == "chronicle-event-sequences-appended-events").Description.ShouldEqual("Number of events appended to the event sequence");
    [Fact] void should_preserve_the_main_empty_append_unit() => _instruments.Single(_ => _.Name == "chronicle-event-sequences-appended-events").Unit.ShouldBeNull();
    [Fact] void should_preserve_the_main_lag_unit() => _instruments.Single(_ => _.Name == "chronicle-event-sequences-constraint-index-lag").Unit.ShouldBeNull();
    [Fact] void should_preserve_the_main_lag_description() => _instruments.Single(_ => _.Name == "chronicle-event-sequences-constraint-index-lag").Description.ShouldEqual("Events not yet covered by confirmed constraint updates");
    [Fact] void should_preserve_the_main_histogram_unit() => _instruments.Single(_ => _.Name == "chronicle-sql-schema-lock-wait").Unit.ShouldEqual("s");
    [Fact] void should_preserve_the_main_histogram_description() => _instruments.Single(_ => _.Name == "chronicle-sql-schema-lock-wait").Description.ShouldEqual("Time spent waiting for a database schema lock");
    [Fact] void should_use_the_main_scope_tag_key() => _measurements.TrueForAll(_ => _.Tags.Contains(new("Namespace", "tenant"))).ShouldBeTrue();
    [Fact] void should_use_the_main_event_tag_key() => _measurements.Single(_ => _.Value == 3).Tags.ShouldContain(new KeyValuePair<string, object?>("eventName", "Event"));
}
