// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Clustering.for_ClusterRoleAssignment.given;

/// <summary>
/// Represents the optional Jobs property supplied by newer kernels.
/// </summary>
public sealed class JobsRoles
{
    /// <summary>
    /// Gets or sets a value indicating whether the silo hosts jobs.
    /// </summary>
    public bool Jobs { get; set; } = true;
}
