// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Defines what a member of a view reads.
/// </summary>
public enum GraphViewMemberKind
{
    /// <summary>
    /// A property of the root node of the view.
    /// </summary>
    Property = 0,

    /// <summary>
    /// A relation along an edge to another view.
    /// </summary>
    Relation = 1,

    /// <summary>
    /// A property of the edge that was followed to reach the view.
    /// </summary>
    EdgeProperty = 2
}
