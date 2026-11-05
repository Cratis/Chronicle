// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_MetricSet;

public class when_using_both_names : given.a_metric_listener
{
    int _initialMeasurements;
    int _createdInstruments;

    void Because()
    {
        var metrics = new MetricSet(_meter, MetricNames.Both, [new(WellKnownTelemetryAttributes.EventStoreNamespace, "tenant")]);
        var appended = metrics.Counter<int>(WellKnownMetricNames.EventSequenceAppendedEvents, WellKnownMetricNames.EventSequenceAppendedEventsUnit, "Number of events appended to the event sequence", WellKnownTelemetryAttributes.EventTypeId);
        var failed = metrics.Counter<int>(WellKnownMetricNames.ObserverPartitionFailures, WellKnownMetricNames.ObserverPartitionFailuresUnit, "Convention failure description");
        var retry = metrics.PartitionRetryAttempts();
        _initialMeasurements = _measurements.Count;
        _createdInstruments = _instruments.Count;
        appended.Record(3, "Event");
        failed.Record(1);
        retry.Record(1);
    }

    [Fact] void should_precreate_all_instruments() => _createdInstruments.ShouldEqual(5);
    [Fact] void should_not_create_instruments_while_recording() => _instruments.Count.ShouldEqual(_createdInstruments);
    [Fact] void should_not_publish_initial_zeros() => _initialMeasurements.ShouldEqual(0);
    [Fact] void should_publish_each_regular_measurement_twice_and_the_legacy_retry_once() => _measurements.Count.ShouldEqual(5);
    [Fact] void should_publish_the_legacy_append_once() => _measurements.Single(_ => _.Name == "chronicle-event-sequences-appended-events").Value.ShouldEqual(3);
    [Fact] void should_publish_the_convention_append_once() => _measurements.Single(_ => _.Name == "cratis.chronicle.event_sequence.appended_events").Value.ShouldEqual(3);
    [Fact] void should_use_legacy_keys_on_the_legacy_append() => _measurements.Single(_ => _.Name == "chronicle-event-sequences-appended-events").Tags.Select(_ => _.Key).ShouldEqual(["Namespace", "eventName"]);
    [Fact] void should_use_convention_keys_on_the_convention_append() => _measurements.Single(_ => _.Name == "cratis.chronicle.event_sequence.appended_events").Tags.Select(_ => _.Key).ShouldEqual(["cratis.event_store.namespace", "cratis.event_type.id"]);
    [Fact] void should_not_repeat_the_legacy_retry_at_the_failure_recording_point() => _measurements.Count(_ => _.Name == "chronicle-observer-partition-retry-attempts").ShouldEqual(1);
    [Fact] void should_keep_the_failure_count_in_each_scheme() => _measurements.Where(_ => _.Name == "chronicle-observer-partitions-failed" || _.Name == "cratis.chronicle.observer.partition_failures").Sum(_ => _.Value).ShouldEqual(2);
}
