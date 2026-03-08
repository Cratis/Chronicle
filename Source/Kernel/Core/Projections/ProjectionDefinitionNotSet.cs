// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// The exception that is thrown when a projection grain is accessed before its definition has been set.
/// </summary>
/// <param name="projectionKey">The <see cref="ProjectionKey"/> of the projection.</param>
public class ProjectionDefinitionNotSet(ProjectionKey projectionKey) : Exception($"Projection definition has not been set for projection with key '{projectionKey}'. Ensure SetDefinition() is called before accessing the projection.")
{
    /// <summary>
    /// Gets the key of the projection that was accessed.
    /// </summary>
    public ProjectionKey ProjectionKey { get; } = projectionKey;
}
