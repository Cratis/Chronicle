// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using OpenTelemetry.Metrics;

namespace Cratis.Kernel.Read.Metrics;

/// <summary>
/// Represents a collection for <see cref="Metric"/>.
/// </summary>
public class MetricCollection : ICollection<MetricSnapshot>
{
    readonly Dictionary<string, MetricSnapshot> _metrics = [];

    readonly Dictionary<string, MetricMeasurement> _measurements = [];

    /// <summary>
    /// Content changed event.
    /// </summary>
    public event MetricCollectionContentChanged ContentChanged = () => { };

    /// <summary>
    /// Gets the collection og <see cref="MetricMeasurement"/>.
    /// </summary>
    public IEnumerable<MetricMeasurement> Measurements => _measurements.Values;

    /// <inheritdoc/>
    public int Count => _metrics.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    public void Add(MetricSnapshot item)
    {
        var sum = item.MetricPoints.Sum(point => item.MetricType switch
        {
            MetricType.LongSum => point.GetSumLong(),
            MetricType.DoubleSum => point.GetSumDouble(),
            MetricType.LongGauge => point.GetGaugeLastValueLong(),
            MetricType.DoubleGauge => point.GetGaugeLastValueDouble(),
            MetricType.Histogram => point.GetHistogramSum(),
            _ => 0
        });

        var points = item.MetricPoints.Select(point =>
        {
            var value = item.MetricType switch
            {
                MetricType.LongSum => point.GetSumLong(),
                MetricType.DoubleSum => point.GetSumDouble(),
                MetricType.LongGauge => point.GetGaugeLastValueLong(),
                MetricType.DoubleGauge => point.GetGaugeLastValueDouble(),
                MetricType.Histogram => point.GetHistogramSum(),
                _ => 0
            };

            var tags = new List<MetricMeasurementPointTag>();
            foreach (var (tag, tagValue) in point.Tags)
            {
                tags.Add(new(tag, tagValue.ToString() ?? string.Empty));
            }

            return new MetricMeasurementPoint(value, tags);
        });

        if (!_measurements.TryGetValue(item.Name, out var value) ||
            value.Aggregated != sum ||
            value.Points.Count() != points.Count())
        {
            value = new MetricMeasurement(item.Name, sum, points);
            _measurements[item.Name] = value;
            ContentChanged();
        }
    }

    /// <inheritdoc/>
    public void Clear() => _metrics.Clear();

    /// <inheritdoc/>
    public bool Contains(MetricSnapshot item) => _metrics.ContainsValue(item);

    /// <inheritdoc/>
    public void CopyTo(MetricSnapshot[] array, int arrayIndex) => _metrics.Values.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public IEnumerator<MetricSnapshot> GetEnumerator() => _metrics.Values.GetEnumerator();

    /// <inheritdoc/>
    public bool Remove(MetricSnapshot item) => _metrics.Remove(item.Name);

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => _metrics.Values.GetEnumerator();
}
