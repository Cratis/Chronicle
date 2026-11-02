// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs.Placement;

/// <summary>
/// Represents role-aware placement for job workers, registered ahead of its use by job steps.
/// </summary>
[Serializable, GenerateSerializer, Immutable, SuppressReferenceTracking]
public class JobWorkerPlacementStrategy : PlacementStrategy
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="JobWorkerPlacementStrategy"/>.
    /// </summary>
    internal static readonly JobWorkerPlacementStrategy Singleton = new();
}
