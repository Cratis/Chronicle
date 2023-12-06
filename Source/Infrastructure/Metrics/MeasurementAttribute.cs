// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Metrics;

/// <summary>
/// Attribute for marking a method as a measurement for the metrics code generator.
/// </summary>
/// <typeparam name="T">Type of counter.</typeparam>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MeasurementAttribute<T> : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="MeasurementAttribute{T}"/>.
    /// </summary>
    /// <param name="name">Name of the counter.</param>
    /// <param name="description">Description of the counter.</param>
    public MeasurementAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Gets the name of the counter.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the description of the counter.
    /// </summary>
    public string Description { get; }
}
