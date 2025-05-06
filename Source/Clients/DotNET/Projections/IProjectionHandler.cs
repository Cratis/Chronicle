// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a system for handling projections.
/// </summary>
public interface IProjectionHandler
{
    /// <summary>
    /// Gets the <see cref="ProjectionDefinition"/>.
    /// </summary>
    ProjectionDefinition Definition { get; }

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
