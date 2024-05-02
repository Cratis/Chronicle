// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Metrics;

/// <summary>
/// Represents a metric measurement point.
/// </summary>
/// <param name="Value">Value of the point.</param>
/// <param name="Tags">Tags for the points.</param>
public record MetricMeasurementPoint(double Value, IEnumerable<MetricMeasurementPointTag> Tags);
