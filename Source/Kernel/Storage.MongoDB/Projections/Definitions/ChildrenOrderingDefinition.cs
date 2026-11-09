// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;

/// <summary>
/// Represents the stored definition of how new children are ordered within a children collection.
/// </summary>
public class ChildrenOrderingDefinition
{
    /// <summary>
    /// Gets or sets where new children are placed.
    /// </summary>
    public ChildrenPlacement Placement { get; set; }

    /// <summary>
    /// Gets or sets the child property path to sort by.
    /// </summary>
    public string SortBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sort direction.
    /// </summary>
    public SortDirection Direction { get; set; }
}
