// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Represents the command for stopping a running job.
/// </summary>
/// <param name="EventStore">The name of the event store the job belongs to.</param>
/// <param name="Namespace">The namespace the job belongs to.</param>
/// <param name="JobId">The unique identifier of the job to stop.</param>
[Command]
public record StopJob(string EventStore, string Namespace, Guid JobId)
{
    internal Task Handle(IGrainFactory grainFactory) =>
        grainFactory.GetJobsManager((EventStoreName)EventStore, (EventStoreNamespaceName)Namespace).Stop((Concepts.Jobs.JobId)JobId);
}
