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
    /// Gets or sets the counters.
    /// </summary>
    public IList<CounterTemplateData> Counters { get; set; } = new List<CounterTemplateData>();
}
