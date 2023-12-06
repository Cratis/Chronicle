// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Roslyn.Extensions.Metrics;

/// <summary>
/// REpresents the template the for metrics.
/// </summary>
public class MetricsTemplateData
{
    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the class name.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the definition of the class.
    /// </summary>
    public string ClassDefinition { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the using statements.
    /// </summary>
    public IList<string> UsingStatements { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the counters.
    /// </summary>
    public IList<MetricTemplateData> Counters { get; set; } = new List<MetricTemplateData>();

    /// <summary>
    /// Gets or sets the measurements.
    /// </summary>
    public IList<MetricTemplateData> Measurements { get; set; } = new List<MetricTemplateData>();
}
