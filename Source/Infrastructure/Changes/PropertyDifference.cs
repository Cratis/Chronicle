// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes;

/// <summary>
/// Represents a value difference in a property of an object.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PropertyDifference"/> class.
/// </remarks>
/// <param name="propertyPath">Raw difference.</param>
/// <param name="original">Original value.</param>
/// <param name="changed">Changed value.</param>
public class PropertyDifference(PropertyPath propertyPath, object? original, object? changed)
{
    /// <summary>
    /// Gets the full member path to the property that has changed.
    /// </summary>
    public PropertyPath PropertyPath { get; } = propertyPath;

    /// <summary>
    /// Gets the original value - possibly default.
    /// </summary>
    public object? Original { get; } = original;

    /// <summary>
    /// Gets the changed value - possibly default.
    /// </summary>
    public object? Changed { get; } = changed;
}
