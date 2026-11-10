// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Alerts.for_AlertIncidentsGauge.given;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Metrics;

namespace Cratis.Chronicle.Observation.for_ObserverGauges.given;

public class the_observer_gauges : Specification
{
    protected System.Diagnostics.Metrics.Meter _meter;
    protected MeterListener _listener;
    protected ObserverGauges _gauges;
    protected List<RecordedMeasurement> _measurements;
    protected ObserverKey _observerKey;
    protected SettableTimeProvider _clock;

    void Establish()
    {
        _meter = new($"observer-gauges-{Guid.NewGuid():N}");
        _measurements = [];
        _listener = new()
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(instrument.Meter, _meter)) listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<int>((instrument, value, tags, _) => _measurements.Add(new(instrument.Name, value, tags.ToArray())));
        _listener.Start();

        var meter = Substitute.For<IMeter<ObserverGauges>>();
        meter.ActualMeter.Returns(_meter);
        _clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        _gauges = new(meter, _clock);
        _observerKey = new("observer", "store", "namespace", "event-log");
    }

    void Destroy()
    {
        _listener.Dispose();
        _meter.Dispose();
    }

    protected IReadOnlyList<RecordedMeasurement> Collect()
    {
        _measurements.Clear();
        _listener.RecordObservableInstruments();
        return [.. _measurements];
    }

    protected IReadOnlyList<RecordedMeasurement> Collect(string instrument) => [.. Collect().Where(_ => _.Instrument == instrument)];

    public record RecordedMeasurement(string Instrument, long Value, IReadOnlyList<KeyValuePair<string, object?>> Tags)
    {
        public IDictionary<string, object?> TagMap => Tags.ToDictionary(_ => _.Key, _ => _.Value);
    }
}
