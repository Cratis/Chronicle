// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Converts the parts of <see cref="KernelBusiness"/> to their wire contracts.
/// </summary>
public static class KernelBusinessConverters
{
    /// <summary>
    /// Convert to the contract.
    /// </summary>
    /// <param name="grain">The busiest grain to convert.</param>
    /// <returns>The contract.</returns>
    public static Contracts.Diagnostics.BusiestGrain ToContract(this BusiestGrain grain) => new()
    {
        GrainType = grain.GrainType,
        EventStore = grain.EventStore,
        Running = grain.Running,
        Pending = grain.Pending,
        OldestPendingAgeSeconds = grain.OldestPendingAgeSeconds
    };

    /// <summary>
    /// Convert to the contract.
    /// </summary>
    /// <param name="jobs">The running job count to convert.</param>
    /// <returns>The contract.</returns>
    public static Contracts.Diagnostics.RunningJobCount ToContract(this RunningJobCount jobs) => new()
    {
        EventStore = jobs.EventStore,
        Namespace = jobs.Namespace,
        JobType = jobs.JobType,
        Count = jobs.Count
    };
}
