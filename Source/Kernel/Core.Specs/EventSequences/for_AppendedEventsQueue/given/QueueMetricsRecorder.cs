// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

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
    /// The <see cref="Meter"/> shared by every recorder.
    /// </summary>
    public static readonly Meter SharedMeter = new("Cratis.Chronicle.Core.Specs.AppendedEventsQueue");

    readonly MeterListener _listener = new();
    readonly ConcurrentQueue<int> _measurements = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueMetricsRecorder"/> class and starts listening.
    /// </summary>
    public QueueMetricsRecorder()
    {
        ObserverId = Guid.NewGuid().ToString();
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (ReferenceEquals(instrument.Meter, SharedMeter) && instrument.Name == SuspectedUndelivered)
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

    /// <inheritdoc/>
    public void Dispose() => _listener.Dispose();

    void OnMeasurement(Instrument instrument, int measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        foreach (var tag in tags)
        {
            if (tag.Key == "ObserverId" && ObserverId.Equals(tag.Value?.ToString(), StringComparison.Ordinal))
            {
                _measurements.Enqueue(measurement);
                if (Throws)
                {
                    throw new InvalidOperationException("The measurement listener failed");
                }

                return;
            }
        }
    }
}
