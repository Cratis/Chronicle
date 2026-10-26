// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

/// <summary>
/// Records the suspected-undelivered measurements a queue reports on its meter, for the one observer the recorder was created for.
/// </summary>
/// <remarks>
/// The queue keeps this counter in a static field bound to the first meter it is used with, so every recorder shares
/// one <see cref="Meter"/> that is never disposed and tells specs apart by the observer id.
/// </remarks>
public sealed class QueueMetricsRecorder : IDisposable
{
    /// <summary>
    /// The name of the instrument counting suspected undelivered events.
    /// </summary>
    public const string SuspectedUndelivered = "chronicle-appended-events-queue-suspected-undelivered-events";

    /// <summary>
    /// The name of the instrument counting observers moved from live delivery to catch-up.
    /// </summary>
    public const string ObserversSpilled = "chronicle-appended-events-queue-observers-spilled-to-catchup";

    /// <summary>
    /// The <see cref="Meter"/> shared by every recorder.
    /// </summary>
    public static readonly Meter SharedMeter = new("Cratis.Chronicle.Core.Specs.AppendedEventsQueue");

    static readonly LegacyMetricNameBridge _bridge = new(meter => ReferenceEquals(meter, SharedMeter), LegacyTelemetryNames.Metrics);

    readonly MeterListener _listener = new();
    readonly ConcurrentQueue<int> _measurements = new();
    readonly ConcurrentQueue<string> _reasons = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueMetricsRecorder"/> class and starts listening.
    /// </summary>
    public QueueMetricsRecorder()
        : this(SuspectedUndelivered)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueMetricsRecorder"/> class for another instrument and starts listening.
    /// </summary>
    /// <param name="instrumentName">The name of the instrument to record.</param>
    /// <param name="observerId">Optional id of the observer to record for; a new one when not given.</param>
    public QueueMetricsRecorder(string instrumentName, string? observerId = null)
    {
        GC.KeepAlive(_bridge);
        ObserverId = observerId ?? Guid.NewGuid().ToString();
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == SharedMeter.Name && instrument.Name == instrumentName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<int>(OnMeasurement);
        _listener.Start();
    }

    /// <summary>
    /// Gets the id of the observer the recorder listens to.
    /// </summary>
    public string ObserverId { get; }

    /// <summary>
    /// Gets or sets whether recording a measurement for the observer throws, the way a failing metric exporter does.
    /// </summary>
    public bool Throws { get; set; }

    /// <summary>
    /// Gets the recorded measurements.
    /// </summary>
    public IEnumerable<int> Measurements => _measurements;

    /// <summary>
    /// Gets the value of the <c language="csharp">Reason</c> tag of every recorded measurement that carried one.
    /// </summary>
    public IEnumerable<string> Reasons => _reasons;

    /// <inheritdoc/>
    public void Dispose() => _listener.Dispose();

    void OnMeasurement(Instrument instrument, int measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        foreach (var tag in tags)
        {
            if (tag.Key == "ObserverId" && ObserverId.Equals(tag.Value?.ToString(), StringComparison.Ordinal))
            {
                _measurements.Enqueue(measurement);
                foreach (var reason in tags)
                {
                    if (reason.Key == "Reason" && reason.Value is not null)
                    {
                        _reasons.Enqueue(reason.Value.ToString()!);
                    }
                }

                if (Throws)
                {
                    throw new InvalidOperationException("The measurement listener failed");
                }

                return;
            }
        }
    }
}
