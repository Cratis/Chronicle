// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Roslyn.Extensions.Metrics;

/// <summary>
/// Represents the template for counters.
/// </summary>
public class CounterTemplateData
{
    /// <summary>
    /// Gets or sets the type of counter.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the counter method.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the counter.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the counter.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tags for the counter.
    /// </summary>
    public IEnumerable<CounterTagTemplateData> Tags { get; set; } = Enumerable.Empty<CounterTagTemplateData>();
}
