// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Reactive;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Api.Jobs;

/// <summary>
/// Represents the API for working with jobs.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for recommendations.</param>
[Route("/api/event-store/{eventStore}/{namespace}/jobs")]
public class JobQueries(IStorage storage) : ControllerBase
{
    /// <summary>
    /// Observes all jobs for a specific event store and namespace.
    /// </summary>
    /// <param name="eventStore">Name of the event store the job is for.</param>
    /// <param name="namespace">Namespace within the event store the job is for.</param>
    /// <returns>An observable for observing a collection of <see cref="JobInformation"/>.</returns>
    [HttpGet]
    public ISubject<IEnumerable<JobInformation>> AllJobs(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace)
    {
        var namespaceStorage = storage.GetEventStore(eventStore).GetNamespace(@namespace);
        var jobs = namespaceStorage.Jobs.ObserveJobs();

        return new TransformingSubject<IEnumerable<JobState>, IEnumerable<JobInformation>>(
            jobs,
            jobs => jobs.Select(_ =>
            {
                return new JobInformation(
                    _.Id,
                    _.Type,
                    _.Name,
                    _.Details,
                    (JobStatus)_.Status,
                    _.StatusChanges.Select(s => new JobStatusChanged(
                        (JobStatus)s.Status,
                        s.Occurred,
                        s.ExceptionMessages,
                        s.ExceptionStackTrace)),
                    new(
                        _.Progress.TotalSteps,
                        _.Progress.SuccessfulSteps,
                        _.Progress.FailedSteps,
                        _.Progress.IsCompleted,
                        _.Progress.Message));
            }).ToArray());
    }

    /// <summary>
    /// Observes all job steps for a specific job and event store and namespace.
    /// </summary>
    /// <param name="eventStore">Name of the event store the job is for.</param>
    /// <param name="namespace">Namespace within the event store the job is for.</param>
    /// <param name="jobId">Identifier of the job to observe for.</param>
    /// <returns>An observable for observing a collection of <see cref="JobStepState"/>.</returns>
    [HttpGet("{jobId}/steps")]
    public ISubject<IEnumerable<JobStepState>> AllJobSteps(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] Guid jobId) =>
        throw new NotImplementedException();
}
