// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Diagnostics.for_MetricSet.given;

public class a_metric_listener_attached_late : Specification
{
    protected Meter _meter;
    protected readonly List<(string Name, int Value)> _measurements = [];
    readonly MeterListener _listener = new();

    void Establish() => _meter = new($"metric-late-listener-{Guid.NewGuid():N}");

    void Destroy()
    {
        _listener.Dispose();
        _meter.Dispose();
    }

    protected void AttachListener()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter == _meter) listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<int>((instrument, value, _, _) => _measurements.Add((instrument.Name, value)));
        _listener.Start();
    }
}
