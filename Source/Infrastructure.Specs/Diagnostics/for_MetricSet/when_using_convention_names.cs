// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_MetricSet;

public class when_using_convention_names : given.a_metric_listener
{
    int _initialMeasurements;

    void Because()
    {
        var metrics = new MetricSet(_meter, MetricNames.Convention, [new(WellKnownTelemetryAttributes.EventStoreNamespace, "tenant")]);
        var appended = metrics.Counter<int>(WellKnownMetricNames.EventSequenceAppendedEvents, WellKnownMetricNames.EventSequenceAppendedEventsUnit, "Number of events appended to the event sequence", WellKnownTelemetryAttributes.EventTypeId);
        var retry = metrics.PartitionRetryAttempts();
        _initialMeasurements = _measurements.Count;
        appended.Record(3, "Event");
        retry.Record(1);
    }

    [Fact] void should_create_only_the_convention_instrument() => _instruments.Single().Name.ShouldEqual("cratis.chronicle.event_sequence.appended_events");
    [Fact] void should_publish_each_measurement_once() => _measurements.Single().Value.ShouldEqual(3);
    [Fact] void should_not_record_initial_zeros() => _initialMeasurements.ShouldEqual(0);
    [Fact] void should_publish_the_convention_unit() => _instruments.Single().Unit.ShouldEqual("{event}");
    [Fact] void should_publish_the_description() => _instruments.Single().Description.ShouldEqual("Number of events appended to the event sequence");
    [Fact] void should_publish_only_convention_tag_keys() => _measurements.Single().Tags.Select(_ => _.Key).ShouldEqual(["cratis.event_store.namespace", "cratis.event_type.id"]);
}
