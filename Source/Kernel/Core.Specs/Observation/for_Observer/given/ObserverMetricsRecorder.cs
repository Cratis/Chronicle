// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.given;

/// <summary>
/// Records the measurements an observer reports on its meter, for the one observer the recorder was created for.
/// </summary>
/// <remarks>
/// The generated metrics keep their instruments in static fields, bound to the first meter they are used with.
/// All recorders therefore share one <see cref="Meter"/> that is never disposed, and tell the observers they listen
/// to apart by the observer id each of them is given - which is also what keeps specs running in parallel from
/// seeing each other's measurements.
/// </remarks>
public sealed class ObserverMetricsRecorder : IDisposable
{
    /// <summary>
    /// The <see cref="Meter"/> shared by every recorder.
    /// </summary>
    public static readonly Meter SharedMeter = new("Cratis.Chronicle.Core.Specs.Observer");

    readonly MeterListener _listener = new();
    readonly ConcurrentQueue<RecordedMeasurement> _measurements = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverMetricsRecorder"/> class and starts listening.
    /// </summary>
    public ObserverMetricsRecorder()
    {
        ObserverId = Guid.NewGuid().ToString();

        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (ReferenceEquals(instrument.Meter, SharedMeter))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<int>(OnMeasurement);
        _listener.Start();
    }

    /// <summary>
    /// Gets the <see cref="ObserverId"/> of the observer the recorder listens to.
    /// </summary>
    public ObserverId ObserverId { get; }

    /// <summary>
    /// Gets the recorded measurements of an instrument.
    /// </summary>
    /// <param name="instrument">The name of the instrument.</param>
    /// <returns>The <see cref="RecordedMeasurement"/>s, in the order they were recorded.</returns>
    public IEnumerable<RecordedMeasurement> For(string instrument) => _measurements.Where(_ => _.Instrument == instrument);

    /// <summary>
    /// Gets the sum of the recorded measurements of an instrument.
    /// </summary>
    /// <param name="instrument">The name of the instrument.</param>
    /// <returns>The sum of the values.</returns>
    public int SumOf(string instrument) => For(instrument).Sum(_ => _.Value);

    /// <inheritdoc/>
    public void Dispose() => _listener.Dispose();

    void OnMeasurement(Instrument instrument, int measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        var recordedTags = tags.ToArray().ToDictionary(_ => _.Key, _ => _.Value);
        if (recordedTags.TryGetValue("ObserverId", out var observerId) && ObserverId.Equals(observerId))
        {
            _measurements.Enqueue(new(instrument.Name, measurement, recordedTags));
        }
    }
}
