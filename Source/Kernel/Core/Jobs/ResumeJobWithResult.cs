// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Represents the command for resuming a paused job and learning whether the job was taken forward.
/// </summary>
/// <remarks>
/// This sits beside <see cref="ResumeJob"/>, which keeps its original wire shape, so that clients can be told why a resume was refused without breaking those built against earlier releases.
/// </remarks>
/// <param name="EventStore">The name of the event store the job belongs to.</param>
/// <param name="Namespace">The namespace the job belongs to.</param>
/// <param name="JobId">The unique identifier of the job to resume.</param>
[Command]
[BelongsTo(WellKnownServices.Jobs)]
public record ResumeJobWithResult(EventStoreName EventStore, EventStoreNamespaceName Namespace, Cratis.Orleans.Jobs.JobId JobId)
{
    /// <summary>
    /// Handles the command by invoking <see cref="Cratis.Orleans.Jobs.IJobsManager.Resume"/> on the jobs manager grain.
    /// </summary>
    /// <param name="grainFactory">The <see cref="Cratis.Orleans.Jobs.IJobsManager"/> to get the jobs manager grain with.</param>
    /// <returns>The <see cref="ResumeJobOutcome"/> telling whether the job was taken forward, and why not when it was not.</returns>
    public async Task<ResumeJobOutcome> Handle(IGrainFactory grainFactory) =>
        JobCommandOutcomes.ForResume(await grainFactory.GetJobsManager(EventStore, Namespace).Resume(JobId));
}
