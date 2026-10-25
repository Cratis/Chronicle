// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Cratis.Metrics;
using Cratis.Traces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

/// <summary>
/// A single subscriber whose queue is activated in a silo, so it records its measurements and warnings like it does in production.
/// </summary>
public class a_single_subscriber_with_queue_telemetry : a_single_subscriber_with_an_event_type
{
    protected readonly QueueMetricsRecorder _recordedMeasurements = new();
    protected readonly RecordingQueueLogger _recordedLogs = new();
    protected TestKitSilo _silo = new();

    protected int _suspectedUndelivered => _recordedMeasurements.Measurements.Sum();

    async Task Establish()
    {
        // The queue of the base given is only constructed, never activated, so it has no metrics scope.
        _queue.Dispose();

        _observerKey = new(_recordedMeasurements.ObserverId, "Some event store", "Some namespace", "Some event sequence");
        _grainFactory.GetGrain<IObserver>(_observerKey).Returns(_observer);

        _silo.AddService(_taskFactory);
        _silo.AddService<IOptions<ChronicleOptions>>(Options.Create(new ChronicleOptions()));
        _silo.AddService<ILogger<AppendedEventsQueue>>(_recordedLogs);
        _silo.AddKeyedService<IMeter<AppendedEventsQueue>>(WellKnown.MeterName, new Meter<AppendedEventsQueue>(QueueMetricsRecorder.SharedMeter));
        _silo.AddKeyedService<IActivitySource<AppendedEventsQueue>>(WellKnown.MeterName, new ActivitySource<AppendedEventsQueue>());
        _silo.AddProbe<IObserver>(_ => _observer);

        var eventSequenceKey = new EventSequenceKey(EventSequenceId.Log, "Some event store", "Some namespace");
        _queue = await _silo.CreateGrainAsync<AppendedEventsQueue>(0L, eventSequenceKey.ToString());
        await _queue.Subscribe(_observerKey, EventTypes, Filters);
    }

    void Destroy() => _recordedMeasurements.Dispose();
}
