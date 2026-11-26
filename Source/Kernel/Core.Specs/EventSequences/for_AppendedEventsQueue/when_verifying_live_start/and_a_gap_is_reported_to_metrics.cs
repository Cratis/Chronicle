// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics;
using Cratis.Metrics;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_verifying_live_start;

public class and_a_gap_is_reported_to_metrics : given.a_queue_verifying_live_gaps
{
    readonly ConcurrentQueue<(string Instrument, int Value, string? Tag)> _measurements = [];
    Meter _meter;
    MeterListener _listener;

    protected override ObserverFilters? LiveFilters => null;

    internal override AppendedEventsQueueMetrics? CreateMetrics()
    {
        _meter = new($"queue-metrics-{Guid.NewGuid():N}");
        _listener = new()
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(instrument.Meter, _meter)) listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<int>((instrument, value, tags, _) =>
            _measurements.Enqueue((instrument.Name, value, tags.ToArray().Select(tag => tag.Value?.ToString()).LastOrDefault())));
        _listener.Start();
        var meter = Substitute.For<IMeter<AppendedEventsQueue>>();
        meter.ActualMeter.Returns(_meter);
        return meter.BeginScope(new EventSequenceKey(_eventSequence, EventStore, Namespace), 0, MetricNames.Convention);
    }

    void Establish() => _history = [EventAt(102)];

    Task Because() => Deliver(EventAt(105));

    void Destroy()
    {
        _listener?.Dispose();
        _meter?.Dispose();
    }

    [Fact] void should_report_a_live_delivery_gap() => Recorded(WellKnownMetricNames.AppendedEventsQueueObserverSpills, AppendedEventsQueueSpillReasons.LiveDeliveryGap).ShouldEqual(1);
    [Fact] void should_not_report_backlog_pressure() => Recorded(WellKnownMetricNames.AppendedEventsQueueObserverSpills, AppendedEventsQueueSpillReasons.LaggingBacklog).ShouldEqual(0);

    int Recorded(string instrument, string tag) => _measurements.Where(_ => _.Instrument == instrument && _.Tag == tag).Sum(_ => _.Value);
}
