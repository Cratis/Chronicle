// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Defines an attribute that maps an event to an edge.
/// </summary>
public interface IEdgeAttribute : IEventBoundAttribute
{
    /// <summary>
    /// Gets the name of the event property that holds the key of the source node.
    /// </summary>
    string SourceKey { get; }

    /// <summary>
    /// Gets the name of the event property that holds the key of the target node.
    /// </summary>
    string TargetKey { get; }

    /// <summary>
    /// Gets the name of the event property that holds the identity of the edge, if the edge type has an identifying property.
    /// </summary>
    string? Identity { get; }
}
