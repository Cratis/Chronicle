// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Diagnostics;
using Cratis.Metrics;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

/// <summary>
/// A queue holding a dormant observer whose own meter is listened to, in the convention names.
/// </summary>
public class a_queue_reporting_its_metrics : a_queue_with_a_stored_durable_subscription
{
    protected readonly ConcurrentQueue<(string Instrument, int Value, string? Tag)> _measurements = [];
    Meter _meter;
    MeterListener _listener;

    internal override AppendedEventsQueueMetrics? CreateMetrics()
    {
        _meter = new($"queue-metrics-{Guid.NewGuid():N}");
        _listener = new()
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(instrument.Meter, _meter))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };
        _listener.SetMeasurementEventCallback<int>((instrument, value, tags, _) =>
            _measurements.Enqueue((instrument.Name, value, tags.ToArray().Select(tag => tag.Value?.ToString()).LastOrDefault())));
        _listener.Start();
        var meter = Substitute.For<IMeter<AppendedEventsQueue>>();
        meter.ActualMeter.Returns(_meter);
        return meter.BeginScope(new EventSequenceKey(_eventSequence, EventStore, Namespace), 0, MetricNames.Convention);
    }

    protected int Recorded(string instrument) => _measurements.Where(measurement => measurement.Instrument == instrument).Sum(measurement => measurement.Value);

    protected int Recorded(string instrument, string tag) => _measurements.Where(measurement => measurement.Instrument == instrument && measurement.Tag == tag).Sum(measurement => measurement.Value);

    void Destroy()
    {
        _listener?.Dispose();
        _meter?.Dispose();
    }
}
