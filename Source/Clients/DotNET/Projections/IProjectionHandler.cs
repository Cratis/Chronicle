// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a system for handling projections.
/// </summary>
public interface IProjectionHandler : IHaveReadModel
{
    /// <summary>
    /// Gets the identifier of the projection.
    /// </summary>
    ProjectionId Id { get; }

    /// <summary>
    /// Get the current state of the reducer.
    /// </summary>
    /// <returns>The current <see cref="ProjectionState"/>.</returns>
    Task<ProjectionState> GetState();

    /// <summary>
    /// Get any failed partitions for a specific reducer.
    /// </summary>
    /// <returns>Collection of <see cref="FailedPartition"/>, if any.</returns>
    Task<IEnumerable<FailedPartition>> GetFailedPartitions();
}
