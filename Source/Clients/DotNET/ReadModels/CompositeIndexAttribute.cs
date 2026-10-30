// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Declares an ordered index over properties of a read model.
/// </summary>
/// <param name="properties">The property names or dotted paths in index order.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class CompositeIndexAttribute(params string[] properties) : Attribute
{
    /// <summary>
    /// Gets the property names or dotted paths in index order.
    /// </summary>
    public string[] Properties { get; } = properties;

    /// <summary>
    /// Gets or sets descending flags in property order. An empty array means all ascending.
    /// </summary>
    public bool[] IsDescending { get; set; } = [];
}
