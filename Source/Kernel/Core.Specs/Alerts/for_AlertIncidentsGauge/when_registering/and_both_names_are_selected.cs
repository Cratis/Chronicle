// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Diagnostics;
using Cratis.Metrics;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsGauge.when_registering;

public class and_both_names_are_selected : Specification
{
    readonly System.Diagnostics.Metrics.Meter _meter = new($"both-gauges-{Guid.NewGuid():N}");
    readonly MeterListener _listener = new();
    readonly List<Instrument> _instruments = [];
    readonly List<(string Name, long Value, KeyValuePair<string, object?>[] Tags)> _measurements = [];
    AlertIncidentsGauge _gauge;
    IEnumerable<Measurement<long>> _firstLegacy;
    IEnumerable<Measurement<long>> _secondLegacy;

    void Establish()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter != _meter) return;
            _instruments.Add(instrument);
            listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => _measurements.Add((instrument.Name, value, tags.ToArray())));
        _listener.Start();
        var meter = new Meter<AlertIncidentsGauge>(_meter);
        _gauge = new(meter, options: Options.Create(new ChronicleOptions { Telemetry = new() { MetricNames = MetricNames.Both } }));
    }

    void Because()
    {
        var owner = new object();
        var holder = (IAlertIncidentsGauge)_gauge;
        holder.Activate(owner);
        holder.Publish(owner, AlertIncidentsGaugeSnapshot.From(DateTimeOffset.UtcNow,
            [(new AlertIncidentGaugeBucket("store", "Default", "observer", "event-log", "partition-failing", Concepts.Alerts.AlertSeverity.Warning), 3L)]));
        _firstLegacy = _gauge.ObserveLegacy();
        _secondLegacy = _gauge.ObserveLegacy();
        _listener.RecordObservableInstruments();
    }

    void Destroy()
    {
        _listener.Dispose();
        _meter.Dispose();
    }

    [Fact] void should_register_both_gauge_pairs() => _instruments.Count.ShouldEqual(4);
    [Fact] void should_reuse_prepared_legacy_measurements_during_collection() => _secondLegacy.ShouldBeSame(_firstLegacy);
    [Fact] void should_publish_the_legacy_value_once() => _measurements.Single(_ => _.Name == "chronicle-alerts-open-incidents").Value.ShouldEqual(3);
    [Fact] void should_publish_the_convention_value_once() => _measurements.Single(_ => _.Name == "cratis.chronicle.alert.open_incidents").Value.ShouldEqual(3);
    [Fact] void should_preserve_the_legacy_unit() => _instruments.Single(_ => _.Name == "chronicle-alerts-open-incidents").Unit.ShouldBeNull();
    [Fact] void should_publish_the_convention_unit() => _instruments.Single(_ => _.Name == "cratis.chronicle.alert.open_incidents").Unit.ShouldEqual("{incident}");
    [Fact] void should_publish_legacy_tag_keys() => _measurements.Single(_ => _.Name == "chronicle-alerts-open-incidents").Tags.Select(_ => _.Key).ShouldEqual(["EventStore", "Namespace", "ObserverId", "EventSequenceId", "Condition", "Severity"]);
    [Fact] void should_publish_convention_tag_keys() => _measurements.Single(_ => _.Name == "cratis.chronicle.alert.open_incidents").Tags.Select(_ => _.Key).ShouldEqual(["cratis.event_store.name", "cratis.event_store.namespace", "cratis.observer.id", "cratis.event_sequence.id", "cratis.chronicle.alert.condition", "cratis.chronicle.alert.severity"]);
}
