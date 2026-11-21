// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Names the property of the root node that a member of a view reads, when it is named differently from the member.
/// </summary>
/// <param name="property">The name of the node property, as it is named in the graph.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class FromNodeAttribute(string property) : Attribute
{
    /// <summary>
    /// Gets the name of the node property.
    /// </summary>
    public string Property { get; } = property;
}
