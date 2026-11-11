// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry;

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Creates a producer's instruments, names and cached tags once, not while recording measurements.
/// </summary>
/// <remarks>
/// The legacy and convention scope tags are built the first time an instrument needs them and are then shared by
/// every instrument of the set, so a set that never creates an instrument holds only the tags it was given.
/// Building them is not synchronized: two threads creating the set's first instruments at once may each build the
/// tags, and one of the equal, never-changed copies wins. Recording through the created instruments is thread-safe.
/// </remarks>
internal sealed class MetricSet
{
    readonly Meter? _meter;
    readonly MetricNames _names;
    readonly IEnumerable<KeyValuePair<string, object?>> _tags;
    MetricScopeTags? _legacyTags;
    MetricScopeTags? _conventionTags;
    int _metricsCreated;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricSet"/> class.
    /// </summary>
    /// <param name="meter">The meter that owns the instruments.</param>
    /// <param name="names">The selected naming mode.</param>
    /// <param name="tags">The scope's convention tag keys and values. They are read when an instrument first needs them, so the collection must not change afterwards.</param>
    internal MetricSet(Meter? meter, MetricNames names, IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        _meter = meter;
        _names = names;
        _tags = tags ?? [];
    }

    /// <summary>
    /// Gets the number of metrics this set has created. A metric published under both names counts once.
    /// </summary>
    /// <remarks>Lets a scope that creates its metrics on first use show it creates each of them only once.</remarks>
    internal int MetricsCreated => Volatile.Read(ref _metricsCreated);

    MetricScopeTags LegacyTags => _legacyTags ??= CreateTags(LegacyTelemetryNames.ToLegacyMetricKey);

    MetricScopeTags ConventionTags => _conventionTags ??= CreateTags(static key => key);

    /// <summary>Creates a counter in the selected naming mode.</summary>
    /// <typeparam name="T">The measurement type.</typeparam>
    /// <param name="name">The convention name.</param>
    /// <param name="unit">The convention unit.</param>
    /// <param name="description">The convention description.</param>
    /// <param name="firstKey">The first additional convention tag key.</param>
    /// <param name="secondKey">The second additional convention tag key.</param>
    /// <returns>The precreated counter or pair of counters.</returns>
    internal MetricInstrument<T, CounterMetricRecorder<T>> Counter<T>(string name, string? unit, string? description = null, string firstKey = "", string secondKey = "")
        where T : struct => Create<T, CounterMetricRecorder<T>>(name, unit, description, firstKey, secondKey, metadata => new CounterMetricRecorder<T>(_meter?.CreateCounter<T>(metadata.Name, metadata.Unit, metadata.Description)));

    /// <summary>Creates a gauge in the selected naming mode.</summary>
    /// <typeparam name="T">The measurement type.</typeparam>
    /// <param name="name">The convention name.</param>
    /// <param name="unit">The convention unit.</param>
    /// <param name="description">The convention description.</param>
    /// <param name="firstKey">The first additional convention tag key.</param>
    /// <param name="secondKey">The second additional convention tag key.</param>
    /// <returns>The precreated gauge or pair of gauges.</returns>
    internal MetricInstrument<T, GaugeMetricRecorder<T>> Gauge<T>(string name, string? unit, string? description = null, string firstKey = "", string secondKey = "")
        where T : struct => Create<T, GaugeMetricRecorder<T>>(name, unit, description, firstKey, secondKey, metadata => new GaugeMetricRecorder<T>(_meter?.CreateGauge<T>(metadata.Name, metadata.Unit, metadata.Description)));

    /// <summary>Creates a histogram in the selected naming mode.</summary>
    /// <typeparam name="T">The measurement type.</typeparam>
    /// <param name="name">The convention name.</param>
    /// <param name="unit">The convention unit.</param>
    /// <param name="description">The convention description.</param>
    /// <param name="firstKey">The first additional convention tag key.</param>
    /// <param name="secondKey">The second additional convention tag key.</param>
    /// <returns>The precreated histogram or pair of histograms.</returns>
    internal MetricInstrument<T, HistogramMetricRecorder<T>> Histogram<T>(string name, string? unit, string? description = null, string firstKey = "", string secondKey = "")
        where T : struct => Create<T, HistogramMetricRecorder<T>>(name, unit, description, firstKey, secondKey, metadata => new HistogramMetricRecorder<T>(_meter?.CreateHistogram<T>(metadata.Name, metadata.Unit, metadata.Description)));

    /// <summary>
    /// Creates a histogram that has no legacy name, published under its convention name and keys in every naming mode.
    /// </summary>
    /// <typeparam name="T">The measurement type.</typeparam>
    /// <param name="name">The convention name.</param>
    /// <param name="unit">The convention unit.</param>
    /// <param name="description">The convention description.</param>
    /// <param name="bucketBoundaries">The explicit bucket boundaries.</param>
    /// <param name="firstKey">The first additional convention tag key.</param>
    /// <returns>The precreated histogram.</returns>
    internal MetricInstrument<T, HistogramMetricRecorder<T>> ConventionOnlyHistogram<T>(string name, string unit, string description, IReadOnlyList<T> bucketBoundaries, string firstKey = "")
        where T : struct
    {
#if NET9_0_OR_GREATER
        var histogram = _meter?.CreateHistogram(name, unit, description, tags: null, advice: new InstrumentAdvice<T> { HistogramBucketBoundaries = bucketBoundaries });
#else
        _ = bucketBoundaries;
        var histogram = _meter?.CreateHistogram<T>(name, unit, description);
#endif
        return Created(new MetricInstrument<T, HistogramMetricRecorder<T>>(
            new SingleMetricInstrument<T, HistogramMetricRecorder<T>>(new HistogramMetricRecorder<T>(histogram), ConventionTags, firstKey, string.Empty)));
    }

    /// <summary>Creates the legacy-only retry counter at its original recording point.</summary>
    /// <returns>The legacy counter, or a no-op in convention mode.</returns>
    internal MetricInstrument<int, CounterMetricRecorder<int>> PartitionRetryAttempts()
    {
        var counter = _names is MetricNames.Convention ? null : _meter?.CreateCounter<int>(
            LegacyTelemetryNames.PartitionRetryAttempts, description: LegacyTelemetryNames.PartitionRetryAttemptsDescription);
        return Created(new MetricInstrument<int, CounterMetricRecorder<int>>(new SingleMetricInstrument<int, CounterMetricRecorder<int>>(
            new CounterMetricRecorder<int>(counter), LegacyTags, string.Empty, string.Empty)));
    }

    MetricInstrument<T, TRecorder> Create<T, TRecorder>(string name, string? unit, string? description, string firstKey, string secondKey, Func<LegacyMetric, TRecorder> create)
        where T : struct
        where TRecorder : struct, IMetricRecorder<T>
    {
        var convention = new LegacyMetric(name, unit, description);
        var legacy = LegacyTelemetryNames.Metrics[name][0];
        return Created(_names switch
        {
            MetricNames.Legacy => new MetricInstrument<T, TRecorder>(CreateLegacy(legacy)),
            MetricNames.Convention => new MetricInstrument<T, TRecorder>(CreateConvention(convention)),
            MetricNames.Both => new MetricInstrument<T, TRecorder>(CreateLegacy(legacy), CreateConvention(convention)),
            _ => throw new InvalidOperationException($"Unsupported metric naming mode: {_names}")
        });

        SingleMetricInstrument<T, TRecorder> CreateLegacy(LegacyMetric metadata) => new(
            create(metadata),
            LegacyTags,
            LegacyTelemetryNames.ToLegacyMetricKey(firstKey),
            LegacyTelemetryNames.ToLegacyMetricKey(secondKey));

        SingleMetricInstrument<T, TRecorder> CreateConvention(LegacyMetric metadata) => new(create(metadata), ConventionTags, firstKey, secondKey);
    }

    TMetric Created<TMetric>(TMetric metric)
    {
        Interlocked.Increment(ref _metricsCreated);
        return metric;
    }

    MetricScopeTags CreateTags(Func<string, string> toKey)
    {
        var tags = default(TagList);
        foreach (var (key, value) in _tags) tags.Add(toKey(key), value);
        return new(tags);
    }
}
