// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Defines the shape of the result of a graph query.
/// </summary>
public enum GraphQueryShape
{
    /// <summary>
    /// Every root that matches, distinct by key and ordered by key ascending.
    /// </summary>
    List = 0,

    /// <summary>
    /// The first root in key order that matches, or nothing: the single result shape.
    /// </summary>
    First = 1
}
