// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Diagnostics.for_MetricSet.given;

public class a_metric_listener : Specification
{
    protected Meter _meter;
    protected readonly List<Instrument> _instruments = [];
    protected readonly List<(string Name, double Value, KeyValuePair<string, object?>[] Tags)> _measurements = [];
    readonly MeterListener _listener = new();

    void Establish()
    {
        _meter = new($"metric-naming-{Guid.NewGuid():N}");
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter != _meter) return;
            _instruments.Add(instrument);
            listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<int>((instrument, value, tags, _) => _measurements.Add((instrument.Name, value, tags.ToArray())));
        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => _measurements.Add((instrument.Name, value, tags.ToArray())));
        _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => _measurements.Add((instrument.Name, value, tags.ToArray())));
        _listener.Start();
    }

    void Destroy()
    {
        _listener.Dispose();
        _meter.Dispose();
    }
}
