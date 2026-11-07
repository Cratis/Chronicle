// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Defines an attribute that points an edge member at the node type at one of its ends.
/// </summary>
public interface IGraphEndpointAttribute
{
    /// <summary>
    /// Gets the node type.
    /// </summary>
    Type NodeType { get; }
}
