// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.given;

/// <summary>
/// Builds stopped retry jobs, as Cratis.Orleans lists them: a job that reached its steps has a status change for it.
/// </summary>
public static class stopped_retry_jobs
{
    /// <summary>
    /// Builds a stopped retry job that was stopped before it was prepared, so Cratis.Orleans refuses to resume it.
    /// </summary>
    /// <param name="observerKey">The observer the job belongs to.</param>
    /// <param name="partition">The partition the job retries.</param>
    /// <param name="episodeId">The optional failure episode that owns the job.</param>
    /// <returns>The <see cref="JobState"/>.</returns>
    public static JobState NeverPrepared(ObserverKey observerKey, Key partition, FailedPartitionId? episodeId = null) => new()
    {
        Id = JobId.New(),
        Status = JobStatus.Stopped,
        Request = RequestFor(observerKey, partition, episodeId)
    };

    /// <summary>
    /// Builds a stopped retry job that had reached its steps before it was stopped.
    /// </summary>
    /// <param name="observerKey">The observer the job belongs to.</param>
    /// <param name="partition">The partition the job retries.</param>
    /// <param name="episodeId">The optional failure episode that owns the job.</param>
    /// <returns>The <see cref="JobState"/>.</returns>
    public static JobState Prepared(ObserverKey observerKey, Key partition, FailedPartitionId? episodeId = null) => new()
    {
        Id = JobId.New(),
        Status = JobStatus.Stopped,
        Request = RequestFor(observerKey, partition, episodeId),
        StatusChanges = [new JobStatusChanged { Status = JobStatus.StartingSteps, Occurred = DateTimeOffset.UtcNow }]
    };

    static RetryFailedPartitionRequest RequestFor(ObserverKey observerKey, Key partition, FailedPartitionId? episodeId) =>
        new(observerKey, ObserverType.Reactor, partition, EventSequenceNumber.First, []) { EpisodeId = episodeId };
}
