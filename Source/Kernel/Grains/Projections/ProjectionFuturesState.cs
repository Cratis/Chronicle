// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents the state for projection futures.
/// </summary>
public class ProjectionFuturesState
{
    /// <summary>
    /// Gets an empty instance of <see cref="ProjectionFuturesState"/>.
    /// </summary>
    public static readonly ProjectionFuturesState Empty = new();

    /// <summary>
    /// Gets the collection of projection futures.
    /// </summary>
    public IList<ProjectionFuture> Futures { get; init; } = [];

    /// <summary>
    /// Gets the collection of just added projection futures.
    /// </summary>
    public IList<ProjectionFuture> AddedFutures { get; set; } = [];

    /// <summary>
    /// Gets the collection of resolved projection futures.
    /// </summary>
    public IList<ProjectionFuture> ResolvedFutures { get; set; } = [];
}
