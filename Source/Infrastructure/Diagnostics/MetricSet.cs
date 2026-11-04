// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry;

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Creates a producer's instruments, names and cached tags once, not while recording measurements.
/// </summary>
internal sealed class MetricSet
{
    readonly Meter? _meter;
    readonly MetricNames _names;
    readonly TagList _legacyTags;
    readonly TagList _conventionTags;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricSet"/> class.
    /// </summary>
    /// <param name="meter">The meter that owns the instruments.</param>
    /// <param name="names">The selected naming mode.</param>
    /// <param name="tags">The scope's convention tag keys and values.</param>
    internal MetricSet(Meter? meter, MetricNames names, IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        _meter = meter;
        _names = names;
        foreach (var (key, value) in tags ?? [])
        {
            _legacyTags.Add(LegacyTelemetryNames.ToLegacyMetricKey(key), value);
            _conventionTags.Add(key, value);
        }
    }

    /// <summary>Creates a counter in the selected naming mode.</summary>
    /// <typeparam name="T">The measurement type.</typeparam>
    /// <param name="name">The convention name.</param>
    /// <param name="unit">The convention unit.</param>
    /// <param name="description">The convention description.</param>
    /// <param name="firstKey">The first additional convention tag key.</param>
    /// <param name="secondKey">The second additional convention tag key.</param>
    /// <returns>The precreated counter or pair of counters.</returns>
    internal IMetricInstrument<T> Counter<T>(string name, string? unit, string? description = null, string firstKey = "", string secondKey = "")
        where T : struct => Create<T>(name, unit, description, firstKey, secondKey, metadata =>
        {
            var counter = _meter?.CreateCounter<T>(metadata.Name, metadata.Unit, metadata.Description);
            return (value, tags) => counter?.Add(value, tags);
        });

    /// <summary>Creates a gauge in the selected naming mode.</summary>
    /// <typeparam name="T">The measurement type.</typeparam>
    /// <param name="name">The convention name.</param>
    /// <param name="unit">The convention unit.</param>
    /// <param name="description">The convention description.</param>
    /// <param name="firstKey">The first additional convention tag key.</param>
    /// <param name="secondKey">The second additional convention tag key.</param>
    /// <returns>The precreated gauge or pair of gauges.</returns>
    internal IMetricInstrument<T> Gauge<T>(string name, string? unit, string? description = null, string firstKey = "", string secondKey = "")
        where T : struct => Create<T>(name, unit, description, firstKey, secondKey, metadata =>
        {
            var gauge = _meter?.CreateGauge<T>(metadata.Name, metadata.Unit, metadata.Description);
            return (value, tags) => gauge?.Record(value, tags);
        });

    /// <summary>Creates a histogram in the selected naming mode.</summary>
    /// <typeparam name="T">The measurement type.</typeparam>
    /// <param name="name">The convention name.</param>
    /// <param name="unit">The convention unit.</param>
    /// <param name="description">The convention description.</param>
    /// <param name="firstKey">The first additional convention tag key.</param>
    /// <param name="secondKey">The second additional convention tag key.</param>
    /// <returns>The precreated histogram or pair of histograms.</returns>
    internal IMetricInstrument<T> Histogram<T>(string name, string? unit, string? description = null, string firstKey = "", string secondKey = "")
        where T : struct => Create<T>(name, unit, description, firstKey, secondKey, metadata =>
        {
            var histogram = _meter?.CreateHistogram<T>(metadata.Name, metadata.Unit, metadata.Description);
            return (value, tags) => histogram?.Record(value, tags);
        });

    /// <summary>Creates the legacy-only retry counter at its original recording point.</summary>
    /// <returns>The legacy counter, or a no-op in convention mode.</returns>
    internal IMetricInstrument<int> PartitionRetryAttempts()
    {
        var counter = _names is MetricNames.Convention ? null : _meter?.CreateCounter<int>(
            LegacyTelemetryNames.PartitionRetryAttempts, description: LegacyTelemetryNames.PartitionRetryAttemptsDescription);
        return new SingleMetricInstrument<int>((value, tags) => counter?.Add(value, tags), _legacyTags, string.Empty, string.Empty);
    }

    IMetricInstrument<T> Create<T>(string name, string? unit, string? description, string firstKey, string secondKey, Func<LegacyMetric, Action<T, TagList>> create)
        where T : struct
    {
        var convention = new LegacyMetric(name, unit, description);
        var legacy = LegacyTelemetryNames.Metrics[name][0];
        return _names switch
        {
            MetricNames.Legacy => CreateSingle(legacy, legacy: true),
            MetricNames.Convention => CreateSingle(convention, legacy: false),
            MetricNames.Both => new DualMetricInstrument<T>(CreateSingle(legacy, legacy: true), CreateSingle(convention, legacy: false)),
            _ => throw new InvalidOperationException($"Unsupported metric naming mode: {_names}")
        };

        IMetricInstrument<T> CreateSingle(LegacyMetric metadata, bool legacy) => new SingleMetricInstrument<T>(
            create(metadata),
            legacy ? _legacyTags : _conventionTags,
            legacy ? LegacyTelemetryNames.ToLegacyMetricKey(firstKey) : firstKey,
            legacy ? LegacyTelemetryNames.ToLegacyMetricKey(secondKey) : secondKey);
    }
}
