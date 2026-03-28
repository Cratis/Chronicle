// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Represents the command for resuming a paused job.
/// </summary>
/// <param name="EventStore">The name of the event store the job belongs to.</param>
/// <param name="Namespace">The namespace the job belongs to.</param>
/// <param name="JobId">The unique identifier of the job to resume.</param>
[Command]
[BelongsTo(WellKnownServices.Jobs)]
public record ResumeJob(string EventStore, string Namespace, Guid JobId)
{
    /// <summary>
    /// Handles the command by invoking <see cref="IJobsManager.Resume"/> on the jobs manager grain.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get jobs manager grains with.</param>
    /// <returns>Awaitable task.</returns>
    internal Task Handle(IGrainFactory grainFactory) =>
        grainFactory.GetJobsManager((EventStoreName)EventStore, (EventStoreNamespaceName)Namespace).Resume((Concepts.Jobs.JobId)JobId);
}
