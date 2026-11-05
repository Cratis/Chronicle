// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Defines a removal attribute that can state whether, for a node in a graph, the edges attached to the node are deleted with it.
/// </summary>
public interface IDetachingRemovedWithAttribute
{
    /// <summary>
    /// Gets a value indicating whether the edges attached to the node are deleted with it.
    /// </summary>
    bool Detach { get; }
}
