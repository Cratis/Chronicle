// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Diagnostics;
using Cratis.Metrics;

namespace Cratis.Chronicle.Observation.for_ObserverMetrics;

public class when_recording_a_metric_for_the_first_time : Specification
{
    const string LegacyName = "chronicle-observer-partitions-failed";
    const string ConventionName = "cratis.chronicle.observer.partition_failures";

    static readonly string[] _legacyTags = ["ObserverId=observer", "EventStore=store", "Namespace=namespace", "EventSequenceId=sequence"];
    static readonly string[] _conventionTags = ["cratis.observer.id=observer", "cratis.event_store.name=store", "cratis.event_store.namespace=namespace", "cratis.event_sequence.id=sequence"];

    readonly System.Diagnostics.Metrics.Meter _meter = new($"first-record-{Guid.NewGuid():N}");
    readonly MeterListener _listener = new();
    readonly List<string> _instruments = [];
    readonly List<(string Name, int Value, string[] Tags)> _measurements = [];

    void Establish()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (!ReferenceEquals(instrument.Meter, _meter)) return;
            _instruments.Add(instrument.Name);
            listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<int>((instrument, value, tags, _) =>
            _measurements.Add((instrument.Name, value, tags.ToArray().Select(_ => $"{_.Key}={_.Value}").ToArray())));
        _listener.Start();
    }

    void Because()
    {
        var metrics = new Meter<Observer>(_meter).BeginObserverScope("observer", new("observer", "store", "namespace", "sequence"), MetricNames.Both);
        metrics.PartitionFailed();
        metrics.PartitionFailed();
    }

    void Destroy()
    {
        _listener.Dispose();
        _meter.Dispose();
    }

    [Fact] void should_create_only_the_recorded_instrument_under_each_name() => _instruments.Order(StringComparer.Ordinal).ShouldEqual([LegacyName, ConventionName]);
    [Fact] void should_record_each_measurement_under_each_name() => _measurements.Select(_ => _.Name).Order(StringComparer.Ordinal).ShouldEqual([LegacyName, LegacyName, ConventionName, ConventionName]);
    [Fact] void should_record_the_value() => _measurements.TrueForAll(_ => _.Value == 1).ShouldBeTrue();
    [Fact] void should_tag_the_legacy_measurements_with_the_legacy_scope_tags() => _measurements.Where(_ => _.Name == LegacyName).All(_ => _.Tags.SequenceEqual(_legacyTags)).ShouldBeTrue();
    [Fact] void should_tag_the_convention_measurements_with_the_convention_scope_tags() => _measurements.Where(_ => _.Name == ConventionName).All(_ => _.Tags.SequenceEqual(_conventionTags)).ShouldBeTrue();
}
