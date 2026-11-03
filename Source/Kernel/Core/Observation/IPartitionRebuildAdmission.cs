// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines the admission check deciding whether a partition replay rebuilds the partition in isolation.
/// </summary>
public interface IPartitionRebuildAdmission
{
    /// <summary>
    /// Decide whether a partition replay is rebuilt in isolation and published atomically.
    /// </summary>
    /// <param name="request">The <see cref="ReplayObserverPartitionRequest"/> describing the replay.</param>
    /// <returns>True if the partition is rebuilt in isolation; false if it is replayed in place.</returns>
    /// <remarks>Fails closed: anything that cannot be established makes the replay run in place.</remarks>
    Task<bool> IsAdmitted(ReplayObserverPartitionRequest request);
}
