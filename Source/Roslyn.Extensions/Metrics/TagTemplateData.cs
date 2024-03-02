// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Roslyn.Extensions.Metrics;

/// <summary>
/// Represents the template for counter tags.
/// </summary>
public class TagTemplateData
{
    /// <summary>
    /// Gets or sets the type of tag.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the tag.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
