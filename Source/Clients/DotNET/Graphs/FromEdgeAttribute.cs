// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Makes a member of a view a property of the edge that was followed to reach the view. Only valid in a view that is
/// the target of a relation.
/// </summary>
/// <param name="property">The name of the edge property, or null to match the member name after the naming policy.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class FromEdgeAttribute(string? property = null) : Attribute
{
    /// <summary>
    /// Gets the name of the edge property, or null to match the member name.
    /// </summary>
    public string? Property { get; } = property;
}
