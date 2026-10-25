// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The observer's catch-up ownership lease: None, then Active (job and generation), then Stopping, then None again.
/// Every transition lives here, so the rule that decides who may register catch-up holds cannot drift (#5406).
/// </summary>
/// <remarks>
/// A generation is only issued while no stop is in progress, so none can be minted between a stop's first and last await.
/// A job that was stopped or deleted is blocked until it is readmitted by an explicit resume. The observer is a
/// single-threaded grain, so no synchronization is needed; the lease is only touched from its turns.
/// </remarks>
public class CatchUpOwnershipLease
{
    readonly HashSet<JobId> _stoppedJobs = [];
    JobId _activeJob = JobId.NotSet;
    Guid _generation = Guid.Empty;
    int _stops;

    /// <summary>
    /// Gets a value indicating whether a stop is in progress.
    /// </summary>
    public bool IsStopping => _stops > 0;

    /// <summary>
    /// Gets the stop revision, fencing acquisitions that awaited across an entire stop.
    /// </summary>
    public int StopRevision { get; private set; }

    /// <summary>
    /// Gets a value indicating whether a job currently holds the lease.
    /// </summary>
    public bool IsActive => _stops == 0 && _generation != Guid.Empty;

    /// <summary>
    /// Issues a generation to a job that is being resumed, readmitting it if it was stopped.
    /// </summary>
    /// <param name="jobId">The resuming job.</param>
    /// <returns>The generation, or <see cref="Guid.Empty"/> while a stop is in progress.</returns>
    public Guid Readmit(JobId jobId)
    {
        if (IsStopping || (IsActive && _activeJob != jobId)) return Guid.Empty;
        _stoppedJobs.Remove(jobId);
        return Issue(jobId);
    }

    /// <summary>
    /// Issues a generation to a job that was found running.
    /// </summary>
    /// <param name="jobId">The running job.</param>
    /// <returns>The generation, or <see cref="Guid.Empty"/> while a stop is in progress or when the job was stopped and not readmitted.</returns>
    public Guid Acquire(JobId jobId) => IsStopping || _stoppedJobs.Contains(jobId) || (IsActive && _activeJob != jobId) ? Guid.Empty : Issue(jobId);

    /// <summary>
    /// Enters Stopping. Call before the stop's first await, and pair with <see cref="EndStop"/> after its last.
    /// Whoever held the lease loses it.
    /// </summary>
    public void BeginStop()
    {
        _stops++;
        StopRevision++;
        if (_activeJob != JobId.NotSet) _stoppedJobs.Add(_activeJob);
        Release();
    }

    /// <summary>
    /// Records jobs that were stopped or deleted, so none of them can register until it is readmitted.
    /// </summary>
    /// <param name="jobIds">The stopped jobs.</param>
    public void MarkStopped(IEnumerable<JobId> jobIds)
    {
        foreach (var jobId in jobIds)
        {
            _stoppedJobs.Add(jobId);
        }
    }

    /// <summary>
    /// Leaves Stopping once the stop's last await has completed. Nested stops return to None only when all have ended.
    /// </summary>
    public void EndStop()
    {
        if (_stops > 0) _stops--;
        Release();
    }

    /// <summary>
    /// Drops the lease because its holder concluded or catch-up moved on. Does not end a stop in progress.
    /// </summary>
    /// <param name="jobId">The concluded job, or <see cref="JobId.NotSet"/> to drop whoever holds it.</param>
    public void Conclude(JobId jobId)
    {
        if (jobId == JobId.NotSet || _activeJob == jobId)
        {
            Release();
        }
    }

    /// <summary>
    /// Drops the lease whoever holds it.
    /// </summary>
    public void Release()
    {
        _activeJob = JobId.NotSet;
        _generation = Guid.Empty;
    }

    /// <summary>
    /// Tells whether a registration is from the job and generation currently holding the lease.
    /// </summary>
    /// <param name="jobId">The registering job.</param>
    /// <param name="generation">The generation it presents.</param>
    /// <returns>True if the lease is Active for exactly that job and generation and the job is not stopped.</returns>
    public bool Accepts(JobId jobId, Guid generation) =>
        IsActive && generation != Guid.Empty && _activeJob == jobId && _generation == generation && !_stoppedJobs.Contains(jobId);

    Guid Issue(JobId jobId)
    {
        _activeJob = jobId;
        _generation = Guid.NewGuid();
        return _generation;
    }
}
