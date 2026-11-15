// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Defines the attribute that marks a type as a view over a property graph.
/// </summary>
public interface IGraphViewAttribute
{
    /// <summary>
    /// Gets the CLR type of the node the view is anchored at.
    /// </summary>
    Type NodeType { get; }

    /// <summary>
    /// Gets the name of the view, or null to name it after the type.
    /// </summary>
    string? Name { get; }
}
