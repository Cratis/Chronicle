// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>
/// Measures append and delivery telemetry, not storage or transport. Run the identical files on main
/// and the candidate with --filter '*TelemetryHotPathBenchmark*' --inProcess (default job).
/// Main has only Legacy metrics, so its Both rows intentionally repeat that baseline.
/// Metric naming does not configure client spans: both rows use default Legacy span naming.
/// </summary>
[MemoryDiagnoser]
[MedianColumn]
public class TelemetryHotPathBenchmark : IDisposable
{
    readonly System.Diagnostics.Metrics.Meter _meter = new("Chronicle.TelemetryHotPathBenchmark");
    readonly MeterListener _meterListener = new();
    readonly TelemetryMetricValidation _validation = new();
    readonly ActivitySource _unobservedSource = new("Chronicle.TelemetryHotPathBenchmark.Unobserved");
    readonly ActivitySource _observedSource = new("Chronicle.TelemetryHotPathBenchmark.Observed");
    readonly ActivityListener _activityListener = new();
    Action<string> _appended = null!;
    Action<int> _enqueued = null!;
    Action _delivered = null!;
    Func<IDisposable> _startNoListener = null!;
    Func<IDisposable> _startWithListener = null!;

    /// <summary>
    /// Gets or sets the kernel metric naming mode selected at activation, never per measurement.
    /// </summary>
    [Params("Legacy", "Both")]
    public string Mode { get; set; } = "Legacy";

    /// <summary>
    /// Attaches listeners and binds production methods without reflection or adapter allocation in the timed path.
    /// </summary>
    /// <exception cref="InvalidOperationException">A benchmark method did not produce the exact expected measurements.</exception>
    [GlobalSetup]
    public void Setup()
    {
        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (ReferenceEquals(instrument.Meter, _meter))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _validation.AttachCallbacks(_meterListener);

        var sequence = TelemetryHotPathAdapter.CreateScope<EventSequence>(
            _meter, ("Cratis.Chronicle.EventSequences.EventSequenceMetricsScopes", "BeginEventSequenceScope"), Mode, new EventStoreName("store"), new EventStoreNamespaceName("namespace"));
        var queue = TelemetryHotPathAdapter.CreateScope<AppendedEventsQueue>(
            _meter, ("Cratis.Chronicle.EventSequences.AppendedEventsQueueMetricsScopes", "BeginScope"), Mode, new EventSequenceKey("log", "store", "namespace"), new AppendedEventsQueueId(0));
        var observer = TelemetryHotPathAdapter.CreateScope<Observer>(
            _meter, ("Cratis.Chronicle.Observation.ObserverMetricsScopes", "BeginObserverScope"), Mode, new ObserverId("observer"), new ObserverKey("observer", "store", "namespace", "log"));
        _appended = TelemetryHotPathAdapter.BindMetric<Action<string>>(sequence, "Cratis.Chronicle.EventSequences.EventSequenceMetrics", "AppendedEvent");
        _enqueued = TelemetryHotPathAdapter.BindMetric<Action<int>>(queue, "Cratis.Chronicle.EventSequences.AppendedEventsQueueMetrics", "EventsEnqueued");
        _delivered = TelemetryHotPathAdapter.BindMetric<Action>(observer, "Cratis.Chronicle.Observation.ObserverMetrics", "SuccessfulObservation");

        // Start after constructing producers to exercise instruments that were initially disabled.
        _meterListener.Start();

        _activityListener.ShouldListenTo = source => ReferenceEquals(source, _observedSource);
        _activityListener.Sample = (ref _) => ActivitySamplingResult.AllData;
        _activityListener.SampleUsingParentId = (ref _) => ActivitySamplingResult.AllData;
        ActivitySource.AddActivityListener(_activityListener);
        _startNoListener = TelemetryHotPathAdapter.BindAppend(_unobservedSource);
        _startWithListener = TelemetryHotPathAdapter.BindAppend(_observedSource);
        ValidateMeasurements();
    }

    /// <summary>
    /// Records one appended event and its enqueue, as on a successful single append.
    /// </summary>
    [Benchmark]
    public void AppendMetrics()
    {
        _appended("CustomerRegistered");
        _enqueued(1);
    }

    /// <summary>
    /// Records a successful observer delivery.
    /// </summary>
    [Benchmark]
    public void ObserverDeliveryMetrics() => _delivered();

    /// <summary>
    /// Starts and disposes the production append scope when tracing is disabled.
    /// </summary>
    [Benchmark]
    public void ClientAppendNoListener() => _startNoListener().Dispose();

    /// <summary>
    /// Starts and disposes the production append scope when a listener samples AllData.
    /// </summary>
    [Benchmark]
    public void ClientAppendWithListener() => _startWithListener().Dispose();

    /// <summary>
    /// Releases all listener and source state between cases.
    /// </summary>
    [GlobalCleanup]
    public void Dispose()
    {
        _activityListener.Dispose();
        _observedSource.Dispose();
        _unobservedSource.Dispose();
        _meterListener.Dispose();
        _meter.Dispose();
        GC.SuppressFinalize(this);
    }

    void ValidateMeasurements()
    {
        var both = Mode == "Both" && TelemetryHotPathAdapter.HasMetricNamingModes;
        string[] appendNames = both
            ? ["chronicle-event-sequences-appended-events", "chronicle-appended-events-queue-events-enqueued", "cratis.chronicle.event_sequence.appended_events", "cratis.chronicle.appended_events_queue.enqueued_events"]
            : ["chronicle-event-sequences-appended-events", "chronicle-appended-events-queue-events-enqueued"];
        string[] deliveryNames = both
            ? ["chronicle-observer-successful-observations", "cratis.chronicle.observer.successful_observations"]
            : ["chronicle-observer-successful-observations"];
        _validation.Verify(AppendMetrics, appendNames);
        _validation.Verify(ObserverDeliveryMetrics, deliveryNames);
        _validation.Verify(ClientAppendNoListener);
        _validation.Verify(ClientAppendWithListener);
    }
}
