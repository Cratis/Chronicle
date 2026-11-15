// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Observation.Webhooks.given;

/// <summary>
/// Records circuit measurements from the shared webhook meter.
/// </summary>
public sealed class WebhookMetricsRecorder : IDisposable
{
    readonly MeterListener _listener = new();
    readonly ConcurrentQueue<RecordedWebhookMeasurement> _measurements = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookMetricsRecorder"/> class.
    /// </summary>
    public WebhookMetricsRecorder()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == WellKnown.MeterName && IsCircuitCounter(instrument.Name))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            var recordedTags = new Dictionary<string, object?>();
            foreach (var tag in tags) recordedTags[tag.Key] = tag.Value;
            _measurements.Enqueue(new(instrument.Name, value, recordedTags));
        });
        _listener.Start();
    }

    /// <summary>
    /// Finds measurements belonging to an isolated host.
    /// </summary>
    /// <param name="host">The unique host.</param>
    /// <returns>The measurements for that host.</returns>
    public IEnumerable<RecordedWebhookMeasurement> ForHost(string host) => _measurements.Where(measurement => measurement.Tags.TryGetValue("authority", out var authority) && authority?.ToString()?.Contains(host, StringComparison.OrdinalIgnoreCase) == true);

    /// <inheritdoc/>
    public void Dispose() => _listener.Dispose();

    static bool IsCircuitCounter(string name) =>
        string.Equals(name, "chronicle-webhook-circuit-opened", StringComparison.Ordinal) ||
        string.Equals(name, "chronicle-webhook-circuit-rejected", StringComparison.Ordinal);
}
