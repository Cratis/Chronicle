// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Defines the direction a relation of a view follows an edge in.
/// </summary>
public enum GraphViewDirection
{
    /// <summary>
    /// From the source of the edge to its target: the root of the view is the source.
    /// </summary>
    Outgoing = 0,

    /// <summary>
    /// From the target of the edge to its source: the root of the view is the target.
    /// </summary>
    Incoming = 1
}
