// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Defines the attribute that makes a member of a view a relation along an edge.
/// </summary>
public interface IFollowsAttribute
{
    /// <summary>
    /// Gets the CLR type of the edge that is followed.
    /// </summary>
    Type EdgeType { get; }

    /// <summary>
    /// Gets the direction the edge is followed in, or null to derive it from the root of the view.
    /// </summary>
    GraphViewDirection? Direction { get; }
}
