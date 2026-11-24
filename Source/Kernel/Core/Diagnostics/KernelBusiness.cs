// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Diagnostics.Load;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// What the silo that answers is busy with: its load status and why, the grains its calls are waiting on the longest,
/// the streams it serves and the observer jobs it runs.
/// </summary>
/// <remarks>
/// The report is the view of the one silo that answers, read from its own memory without calling a grain, so it answers
/// while the grains are saturated. Ask each silo to see the whole cluster.
/// </remarks>
/// <param name="Silo">The address of the silo that answered.</param>
/// <param name="Status">The load status of the silo.</param>
/// <param name="Reasons">Why the silo is degraded: one entry per limit crossed. Empty when it is not.</param>
/// <param name="BusiestGrains">The grain types the silo's calls are waiting on or running on the most, the one with the oldest queued call first.</param>
/// <param name="OpenStreams">How many gRPC streaming calls the silo is serving.</param>
/// <param name="RunningJobs">The observer jobs running on the silo, counted per event store, namespace and job type.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Diagnostics)]
[Authorize(Policy = WellKnownAuthorizationPolicies.Administrator)]
public record KernelBusiness(
    string Silo,
    KernelLoadStatus Status,
    IEnumerable<string> Reasons,
    IEnumerable<BusiestGrain> BusiestGrains,
    int OpenStreams,
    IEnumerable<RunningJobCount> RunningJobs)
{
    /// <summary>
    /// Gets what the silo is busy with.
    /// </summary>
    /// <param name="load">The <see cref="IKernelLoad"/> of the silo.</param>
    /// <param name="jobs">The <see cref="IObserverJobGauges"/> holding the running observer jobs.</param>
    /// <param name="localSiloDetails">The <see cref="ILocalSiloDetails"/> of the silo.</param>
    /// <returns>The <see cref="KernelBusiness"/> report.</returns>
    public static Task<KernelBusiness> GetKernelBusiness(IKernelLoad load, IObserverJobGauges jobs, ILocalSiloDetails localSiloDetails)
    {
        var report = load.Report();
        return Task.FromResult(new KernelBusiness(
            localSiloDetails.SiloAddress.ToString(),
            report.Status,
            report.Reasons,
            report.Grains.Select(_ => new BusiestGrain(_.GrainType, _.EventStore, _.Running, _.Pending, _.OldestPendingAge.TotalSeconds)).ToArray(),
            report.OpenStreams,
            jobs.Running().Select(_ => new RunningJobCount(_.EventStore, _.Namespace, _.JobType, _.Count)).ToArray()));
    }
}
