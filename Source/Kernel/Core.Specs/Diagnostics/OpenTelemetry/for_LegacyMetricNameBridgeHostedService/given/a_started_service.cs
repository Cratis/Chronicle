// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_LegacyMetricNameBridgeHostedService.given;

public class a_started_service : Specification
{
    protected readonly List<int> _measurements = [];
    protected readonly KeyValuePair<string, object?> _marker = new("marker", Guid.NewGuid().ToString("N"));
    private protected LegacyMetricNameBridgeHostedService _service;
    protected Gauge<int> _counter;
    Meter _meter;
    MeterListener _listener;

    async Task Establish()
    {
        _meter = new(WellKnown.MeterName);
        _listener = new()
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Name == "chronicle-connected-clients") listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<int>((_, value, tags, _) =>
        {
            if (tags.ToArray().Contains(_marker)) _measurements.Add(value);
        });
        _listener.Start();
        _counter = _meter.CreateGauge<int>(WellKnownMetricNames.ConnectedClientCount);
        _service = new();
        await _service.StartAsync(CancellationToken.None);
    }

    void Destroy()
    {
        _service.Dispose();
        _listener.Dispose();
        _meter.Dispose();
    }
}
