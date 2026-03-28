// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649, MA0048

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Jobs;

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
public record Job(
    Guid Id,
    string Details,
    string Type,
    JobStatus Status,
    DateTimeOffset Created,
    IEnumerable<JobStatusChanged> StatusChanges,
    JobProgress Progress)
{
    /// <summary>
    /// Observes all jobs for the given event store and namespace.
    /// </summary>
    /// <param name="eventStore">Name of the event store the job is for.</param>
    /// <param name="namespace">Namespace within the event store the job is for.</param>
    /// <param name="storage">The <see cref="IStorage"/> to observe jobs from.</param>
    /// <returns>An observable subject emitting collections of jobs.</returns>
    internal static ISubject<IEnumerable<Job>> AllJobs(string eventStore, string @namespace, IStorage storage)
    {
        var catchOrObserve = storage
            .GetEventStore(eventStore)
            .GetNamespace(@namespace).Jobs
            .ObserveJobs();

        if (catchOrObserve.IsSuccess)
        {
            return catchOrObserve.AsT0.TransformSubject(ToJobs);
        }

        return new ReplaySubject<IEnumerable<Job>>(1);
    }

    private static IEnumerable<Job> ToJobs(IEnumerable<JobState> jobs) => jobs.Select(ToJob);

    private static Job ToJob(JobState job) =>
        new(
            (Guid)job.Id,
            job.Details,
            job.Type,
            (JobStatus)(int)job.Status,
            job.Created,
            job.StatusChanges.Select(ToStatusChanged),
            ToProgress(job.Progress));

    private static JobStatusChanged ToStatusChanged(Concepts.Jobs.JobStatusChanged sc) =>
        new()
        {
            Status = (JobStatus)(int)sc.Status,
            Occurred = sc.Occurred,
            ExceptionMessages = sc.ExceptionMessages.ToList(),
            ExceptionStackTrace = sc.ExceptionStackTrace
        };

    private static JobProgress ToProgress(Concepts.Jobs.JobProgress p) =>
        new()
        {
            TotalSteps = p.TotalSteps,
            SuccessfulSteps = p.SuccessfulSteps,
            FailedSteps = p.FailedSteps,
            StoppedSteps = p.StoppedSteps,
            IsCompleted = p.IsCompleted,
            IsStopped = p.IsStopped,
            Message = p.Message
        };
}
