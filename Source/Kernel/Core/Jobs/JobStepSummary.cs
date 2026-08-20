// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Represents the read model for a job step, providing query access to the job step state store.
/// </summary>
/// <param name="Id">The unique identifier for the job step.</param>
/// <param name="Type">The type identifier of the job step.</param>
/// <param name="Name">The name of the job step.</param>
/// <param name="Status">The current status of the job step.</param>
/// <param name="StatusChanges">History of status changes for the job step.</param>
/// <param name="Progress">The current progress of the job step.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Jobs)]
public record JobStepSummary(
    Guid Id,
    string Type,
    string Name,
    JobStepStatus Status,
    IEnumerable<JobStepStatusChanged> StatusChanges,
    JobStepProgress Progress)
{
    /// <summary>
    /// Gets all job steps for a given job from the given event store and namespace.
    /// </summary>
    /// <param name="eventStore">Name of the event store the job is for.</param>
    /// <param name="namespace">Namespace within the event store the job is for.</param>
    /// <param name="jobId">The unique identifier of the job.</param>
    /// <param name="storage">The <see cref="IStorage"/> to read job steps from.</param>
    /// <returns>A collection of job step summaries.</returns>
    internal static async Task<IEnumerable<JobStepSummary>> GetJobSteps(
        string eventStore,
        string @namespace,
        Guid jobId,
        IStorage storage)
    {
        var result = await storage
            .GetEventStore(eventStore)
            .GetNamespace(@namespace).JobSteps
            .GetForJob(jobId);

        if (result.IsSuccess)
        {
            return result.AsT0.Select(JobStepSummaryConverters.ToJobStep);
        }

        result.TryGetException(out var exception);
        throw exception!;
    }
}
