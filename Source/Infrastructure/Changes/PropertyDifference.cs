// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Changes;

/// <summary>
/// Represents a value difference in a property of an object.
/// </summary>
public class PropertyDifference
{
    /// <summary>
    /// Gets the full member path to the property that has changed.
    /// </summary>
    public PropertyPath PropertyPath { get; }

    /// <summary>
    /// Gets the original value - possibly default.
    /// </summary>
    public object? Original { get; }

    /// <summary>
    /// Gets the changed value - possibly default.
    /// </summary>
    public object? Changed { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyDifference"/> class.
    /// </summary>
    /// <param name="propertyPath">Raw difference.</param>
    /// <param name="original">Original value.</param>
    /// <param name="changed">Changed value.</param>
    public PropertyDifference(PropertyPath propertyPath, object? original, object? changed)
    {
        PropertyPath = propertyPath;
        Original = original;
        Changed = changed;
    }
}
