// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views;

/// <summary>
/// Represents the direction a relation follows an edge, seen from the root of the view.
/// </summary>
public enum GraphViewDirection
{
    /// <summary>
    /// From the source of the edge to its target.
    /// </summary>
    Outgoing = 0,

    /// <summary>
    /// From the target of the edge to its source.
    /// </summary>
    Incoming = 1
}
