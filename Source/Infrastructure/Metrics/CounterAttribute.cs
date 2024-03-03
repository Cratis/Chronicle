// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Metrics;

/// <summary>
/// Attribute for marking a method as a counter for the metrics code generator.
/// </summary>
/// <typeparam name="T">Type of counter.</typeparam>
/// <remarks>
/// Initializes a new instance of <see cref="CounterAttribute{T}"/>.
/// </remarks>
/// <param name="name">Name of the counter.</param>
/// <param name="description">Description of the counter.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CounterAttribute<T>(string name, string description) : Attribute
{
    /// <summary>
    /// Gets the name of the counter.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the description of the counter.
    /// </summary>
    public string Description { get; } = description;
}
