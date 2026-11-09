// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the definition of how new children are ordered within a children collection.
/// </summary>
[ProtoContract]
public class ChildrenOrderingDefinition
{
    /// <summary>
    /// Gets or sets where new children are placed.
    /// </summary>
    [ProtoMember(1)]
    public ChildrenPlacement Placement { get; set; }

    /// <summary>
    /// Gets or sets the child property path to sort by. Required when <see cref="Placement"/> is <see cref="ChildrenPlacement.Sorted"/>.
    /// </summary>
    [ProtoMember(2)]
    public string SortBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sort direction. Used when <see cref="Placement"/> is <see cref="ChildrenPlacement.Sorted"/>.
    /// </summary>
    [ProtoMember(3)]
    public SortDirection Direction { get; set; }
}
