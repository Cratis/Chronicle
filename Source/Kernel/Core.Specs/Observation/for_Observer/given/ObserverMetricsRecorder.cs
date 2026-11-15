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
/// All recorders share one <see cref="Meter"/> that is never disposed, and distinguish observers by their ids
/// so specs running in parallel do not see each other's measurements.
/// </remarks>
public sealed class ObserverMetricsRecorder : IDisposable
{
    /// <summary>
    /// The <see cref="Meter"/> shared by every recorder.
    /// </summary>
    public static readonly Meter SharedMeter = new("Cratis.Chronicle.Core.Specs.Observer");

    readonly MeterListener _listener = new();
    readonly ConcurrentQueue<RecordedMeasurement> _measurements = new();
    readonly ConcurrentDictionary<string, Instrument> _instruments = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverMetricsRecorder"/> class and starts listening.
    /// </summary>
    public ObserverMetricsRecorder()
    {
        ObserverId = Guid.NewGuid().ToString();

        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == SharedMeter.Name && !instrument.Name.StartsWith("cratis.chronicle.", StringComparison.Ordinal))
            {
                _instruments[instrument.Name] = instrument;
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
    /// Gets an instrument that was created on the shared meter.
    /// </summary>
    /// <param name="instrument">The name of the instrument.</param>
    /// <returns>The <see cref="Instrument"/>, or null if it has not been created.</returns>
    public Instrument? InstrumentNamed(string instrument) => _instruments.GetValueOrDefault(instrument);

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
