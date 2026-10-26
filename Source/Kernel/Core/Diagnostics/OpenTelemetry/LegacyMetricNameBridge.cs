// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry;

/// <summary>
/// Mirrors every measurement on a convention-named instrument onto its legacy-named instrument(s), with the legacy
/// attribute keys, so dashboards keep working for one minor release. Removed in the next major version.
/// </summary>
/// <remarks>
/// The legacy instruments live on a meter owned by the bridge that carries the same name as the source meter, so the
/// source meter is never modified. Observable gauges cannot be mirrored, since they have no measurement callback until
/// collection, and are registered twice by their owner instead.
/// </remarks>
internal sealed class LegacyMetricNameBridge : IDisposable
{
    readonly Func<Meter, bool> _selector;
    readonly IReadOnlyDictionary<string, LegacyMetric[]> _map;
    readonly MeterListener _listener = new();
    readonly Dictionary<string, Meter> _legacyMeters = [];
    readonly Lock _lock = new();
    bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyMetricNameBridge"/> class and starts mirroring.
    /// </summary>
    /// <param name="selector">Selects the meters to mirror. Select only meters that the owner of the bridge owns.</param>
    /// <param name="map">The legacy metrics keyed by the convention metric name.</param>
    internal LegacyMetricNameBridge(Func<Meter, bool> selector, IReadOnlyDictionary<string, LegacyMetric[]> map)
    {
        _selector = selector;
        _map = map;
        _listener.InstrumentPublished = OnInstrumentPublished;
        _listener.SetMeasurementEventCallback<int>(OnMeasurement);
        _listener.SetMeasurementEventCallback<long>(OnMeasurement);
        _listener.SetMeasurementEventCallback<double>(OnMeasurement);
        _listener.Start();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        _listener.Dispose();
        foreach (var meter in _legacyMeters.Values)
        {
            meter.Dispose();
        }
    }

    static Instrument? CreateLegacy(Meter meter, Instrument source, LegacyMetric legacy) => source switch
    {
        Counter<int> => meter.CreateCounter<int>(legacy.Name, legacy.Unit, legacy.Description),
        Counter<long> => meter.CreateCounter<long>(legacy.Name, legacy.Unit, legacy.Description),
        Counter<double> => meter.CreateCounter<double>(legacy.Name, legacy.Unit, legacy.Description),
        Histogram<int> => meter.CreateHistogram<int>(legacy.Name, legacy.Unit, legacy.Description),
        Histogram<long> => meter.CreateHistogram<long>(legacy.Name, legacy.Unit, legacy.Description),
        Histogram<double> => meter.CreateHistogram<double>(legacy.Name, legacy.Unit, legacy.Description),
        Gauge<int> => meter.CreateGauge<int>(legacy.Name, legacy.Unit, legacy.Description),
        Gauge<long> => meter.CreateGauge<long>(legacy.Name, legacy.Unit, legacy.Description),
        Gauge<double> => meter.CreateGauge<double>(legacy.Name, legacy.Unit, legacy.Description),
        _ => null
    };

    static void Record<T>(Instrument legacy, T measurement, in TagList tags)
        where T : struct
    {
        switch (legacy)
        {
            case Counter<T> counter:
                counter.Add(measurement, tags);
                break;
            case Histogram<T> histogram:
                histogram.Record(measurement, tags);
                break;
            case Gauge<T> gauge:
                gauge.Record(measurement, tags);
                break;
        }
    }

    void OnInstrumentPublished(Instrument instrument, MeterListener listener)
    {
        if (!_selector(instrument.Meter) || !_map.TryGetValue(instrument.Name, out var legacyMetrics))
        {
            return;
        }

        var legacyMeter = GetLegacyMeter(instrument.Meter);
        if (legacyMeter is null)
        {
            return;
        }

        var instruments = legacyMetrics
            .Select(legacy => CreateLegacy(legacyMeter, instrument, legacy))
            .OfType<Instrument>()
            .ToArray();

        if (instruments.Length > 0)
        {
            listener.EnableMeasurementEvents(instrument, instruments);
        }
    }

    Meter? GetLegacyMeter(Meter source)
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return null;
            }

            if (!_legacyMeters.TryGetValue(source.Name, out var meter))
            {
                meter = _legacyMeters[source.Name] = new Meter(source.Name, source.Version);
            }

            return meter;
        }
    }

    void OnMeasurement<T>(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        where T : struct
    {
        if (state is not Instrument[] legacyInstruments)
        {
            return;
        }

        var translated = default(TagList);
        foreach (var (key, value) in tags)
        {
            translated.Add(LegacyTelemetryNames.ToLegacyMetricKey(key), value);
        }

        foreach (var legacy in legacyInstruments)
        {
            Record(legacy, measurement, translated);
        }
    }
}
