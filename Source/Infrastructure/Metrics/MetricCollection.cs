// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using OpenTelemetry.Metrics;

namespace Cratis.Metrics;

/// <summary>
/// Represents a collection for <see cref="Metric"/>.
/// </summary>
public class MetricCollection : ICollection<MetricSnapshot>
{
    readonly IDictionary<string, MetricSnapshot> _metrics = new Dictionary<string, MetricSnapshot>();

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

        if (!_measurements.ContainsKey(item.Name) ||
            _measurements[item.Name].Aggregated != sum ||
            _measurements[item.Name].Points.Count() != points.Count())
        {
            _measurements[item.Name] = new MetricMeasurement(item.Name, sum, points);
            ContentChanged();
        }
    }

    /// <inheritdoc/>
    public void Clear() => _metrics.Clear();

    /// <inheritdoc/>
    public bool Contains(MetricSnapshot item) => _metrics.Values.Contains(item);

    /// <inheritdoc/>
    public void CopyTo(MetricSnapshot[] array, int arrayIndex) => _metrics.Values.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public IEnumerator<MetricSnapshot> GetEnumerator() => _metrics.Values.GetEnumerator();

    /// <inheritdoc/>
    public bool Remove(MetricSnapshot item) => _metrics.Remove(item.Name);

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => _metrics.Values.GetEnumerator();
}
