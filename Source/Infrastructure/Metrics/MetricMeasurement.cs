// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Metrics;

/// <summary>
/// Represents a metric measurement.
/// </summary>
/// <param name="Name">Name of metric.</param>
/// <param name="Aggregated">Aggregated value.</param>
/// <param name="Points">Measurement points.</param>
public record MetricMeasurement(string Name, double Aggregated, IEnumerable<MetricMeasurementPoint> Points);
