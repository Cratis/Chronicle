// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Represents the command for stopping a running job and learning whether the job took the stop.
/// </summary>
/// <remarks>
/// This sits beside <see cref="StopJob"/>, which keeps its original wire shape, so that clients can be told why a stop was refused without breaking those built against earlier releases.
/// </remarks>
/// <param name="EventStore">The name of the event store the job belongs to.</param>
/// <param name="Namespace">The namespace the job belongs to.</param>
/// <param name="JobId">The unique identifier of the job to stop.</param>
[Command]
[Cratis.Arc.Authorization.Authorize(Policy = Security.WellKnownAuthorizationPolicies.AdministratorOperations)]
[BelongsTo(WellKnownServices.Jobs)]
public record StopJobWithResult(EventStoreName EventStore, EventStoreNamespaceName Namespace, Cratis.Orleans.Jobs.JobId JobId)
{
    /// <summary>
    /// Handles the command by invoking <see cref="Cratis.Orleans.Jobs.IJobsManager.Stop"/> on the jobs manager grain.
    /// </summary>
    /// <param name="grainFactory">The <see cref="Cratis.Orleans.Jobs.IJobsManager"/> to get the jobs manager grain with.</param>
    /// <returns>The <see cref="StopJobOutcome"/> telling whether the job took the stop, and why not when it did not.</returns>
    public async Task<StopJobOutcome> Handle(IGrainFactory grainFactory) =>
        JobCommandOutcomes.ForStop(await grainFactory.GetJobsManager(EventStore, Namespace).Stop(JobId));
}
