// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Represents the read model for a job, providing query access to the job state store.
/// </summary>
/// <param name="Id">The unique identifier for the job.</param>
/// <param name="Details">Descriptive details about the job.</param>
/// <param name="Type">The type identifier of the job.</param>
/// <param name="Status">The current status of the job.</param>
/// <param name="Created">When the job was created.</param>
/// <param name="StatusChanges">History of status changes for the job.</param>
/// <param name="Progress">The current progress of the job.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Jobs)]
public record JobSummary(
    Guid Id,
    string Details,
    string Type,
    JobStatus Status,
    DateTimeOffset Created,
    IEnumerable<JobStatusChanged> StatusChanges,
    JobProgress Progress)
{
    /// <summary>
    /// Gets all jobs for the given event store and namespace.
    /// </summary>
    /// <param name="eventStore">Name of the event store the job is for.</param>
    /// <param name="namespace">Namespace within the event store the job is for.</param>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get the jobs manager grain with.</param>
    /// <returns>A collection of jobs.</returns>
    /// <remarks>
    /// This reads through the jobs manager grain rather than observing storage. Observing goes through the
    /// Arc MongoDB reactive collection, which is only available in a host that configured Arc - the kernel
    /// runs in-process without one in the client integration fixtures.
    /// </remarks>
    internal static async Task<IEnumerable<JobSummary>> AllJobs(EventStoreName eventStore, EventStoreNamespaceName @namespace, IGrainFactory grainFactory)
    {
        var jobs = await grainFactory.GetJobsManager(eventStore, @namespace).GetAllJobs();
        return JobSummaryConverters.ToJobs(jobs);
    }

    /// <summary>
    /// Observes all jobs for the given event store and namespace.
    /// </summary>
    /// <param name="eventStore">Name of the event store the job is for.</param>
    /// <param name="namespace">Namespace within the event store the job is for.</param>
    /// <param name="storage">The <see cref="IStorage"/> to observe jobs from.</param>
    /// <returns>An observable subject emitting collections of jobs.</returns>
    internal static ISubject<IEnumerable<JobSummary>> ObserveJobs(EventStoreName eventStore, EventStoreNamespaceName @namespace, IStorage storage)
    {
        var catchOrObserve = storage
            .GetEventStore(eventStore)
            .GetNamespace(@namespace).Jobs
            .ObserveJobs();

        if (catchOrObserve.IsSuccess)
        {
            return catchOrObserve.AsT0.TransformSubject(JobSummaryConverters.ToJobs);
        }

        catchOrObserve.TryGetException(out var exception);
        throw exception!;
    }
}
