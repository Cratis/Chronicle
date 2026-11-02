// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Placement;

namespace Cratis.Chronicle.Jobs.Placement;

/// <summary>
/// Marks a grain for role-aware job worker placement. Not yet applied to job steps.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class JobWorkerPlacementAttribute : PlacementAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JobWorkerPlacementAttribute"/> class.
    /// </summary>
    public JobWorkerPlacementAttribute()
        : base(JobWorkerPlacementStrategy.Singleton)
    {
    }
}
