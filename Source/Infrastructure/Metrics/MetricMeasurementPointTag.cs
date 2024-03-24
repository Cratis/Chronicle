// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Metrics;

/// <summary>
/// Represents a metric measurement point tags.
/// </summary>
/// <param name="Tag">Name of tag.</param>
/// <param name="Value">Value of the tag.</param>
public record MetricMeasurementPointTag(string Tag, string Value);
