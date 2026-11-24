// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Accepts the commands an operator gives an observer as jobs.
/// </summary>
/// <remarks>
/// Accepting asks the jobs manager, never the observer, so it is answered at once however busy the observer is. The same
/// command given again while its job is unfinished joins that job, also when two are given at the very same moment on one
/// silo (see <see cref="Accept(IJobsManager, ObserverKey, ObserverOperatorCommand, string?, bool)"/>).
/// </remarks>
public static class ObserverOperatorCommandJobs
{
    static readonly ConcurrentDictionary<(EventStoreName EventStore, EventStoreNamespaceName Namespace), SemaphoreSlim> _gates = [];

    /// <summary>
    /// Accepts a replay of an observer, unless the observer cannot be replayed.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get the jobs manager with.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the definition of the observer.</param>
    /// <param name="observerKey">The <see cref="ObserverKey"/> of the observer to replay.</param>
    /// <returns>The <see cref="JobId"/> of the job carrying out the replay; <see cref="JobId.NotSet"/> if the observer cannot be replayed or no job could be started.</returns>
    /// <remarks>
    /// Whether the observer can be replayed is read from its definition, as the observer grain would answer it. Whether it is
    /// quarantined or has no client is only known to the observer; the job fails then, and says so.
    /// </remarks>
    public static async Task<JobId> AcceptReplay(IGrainFactory grainFactory, IStorage storage, ObserverKey observerKey)
    {
        var definition = await storage.GetEventStore(observerKey.EventStore).Observers.Get(observerKey.ObserverId);
        return definition is { IsReplayable: true }
            ? await Accept(grainFactory, observerKey, ObserverOperatorCommand.Replay)
            : JobId.NotSet;
    }

    /// <summary>
    /// Accepts a command for an observer.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get the jobs manager with.</param>
    /// <param name="observerKey">The <see cref="ObserverKey"/> of the observer the command is for.</param>
    /// <param name="command">The <see cref="ObserverOperatorCommand"/> to carry out.</param>
    /// <param name="partition">The partition the command is for, for the commands that are for one partition.</param>
    /// <param name="retryImmediately">Whether clearing the quarantine of a partition also retries it.</param>
    /// <returns>The <see cref="JobId"/> of the job carrying out the command; <see cref="JobId.NotSet"/> if no job could be started.</returns>
    public static Task<JobId> Accept(IGrainFactory grainFactory, ObserverKey observerKey, ObserverOperatorCommand command, string? partition = null, bool retryImmediately = false) =>
        Accept(grainFactory.GetJobsManager(observerKey.EventStore, observerKey.Namespace), observerKey, command, partition, retryImmediately);

    /// <summary>
    /// Accepts a command for an observer.
    /// </summary>
    /// <param name="jobsManager">The <see cref="IJobsManager"/> of the namespace of the observer.</param>
    /// <param name="observerKey">The <see cref="ObserverKey"/> of the observer the command is for.</param>
    /// <param name="command">The <see cref="ObserverOperatorCommand"/> to carry out.</param>
    /// <param name="partition">The partition the command is for, for the commands that are for one partition.</param>
    /// <param name="retryImmediately">Whether clearing the quarantine of a partition also retries it.</param>
    /// <returns>The <see cref="JobId"/> of the job carrying out the command; <see cref="JobId.NotSet"/> if no job could be started.</returns>
    /// <remarks>
    /// Finding the job to join and starting one is one step at a time per namespace on this silo: a job being started is
    /// not listed by the jobs manager until its start completes, so two identical commands arriving together would both
    /// find nothing and start a job each. Identical commands arriving on different silos at the same moment can still start
    /// one job each; the observer carries out the second as a no-op, because it refuses what is already running.
    /// </remarks>
    internal static async Task<JobId> Accept(IJobsManager jobsManager, ObserverKey observerKey, ObserverOperatorCommand command, string? partition = null, bool retryImmediately = false)
    {
        var request = new ObserverOperatorCommandRequest(observerKey, command, partition, retryImmediately);
        var gate = _gates.GetOrAdd((observerKey.EventStore, observerKey.Namespace), _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            var same = (await jobsManager.GetOutstandingJobsOf<IObserverOperatorCommandJob>())
                .Where(job => job.Request is ObserverOperatorCommandRequest outstanding && outstanding == request)
                .ToArray();

            var now = DateTimeOffset.UtcNow;
            var running = same.FirstOrDefault(job => job.IsLive(now));
            if (running is not null)
            {
                return running.Id;
            }

            foreach (var stopped in same.Where(job => job.Status == JobStatus.Stopped))
            {
                if (await jobsManager.Resume(stopped.Id))
                {
                    return stopped.Id;
                }
            }

            var started = await jobsManager.Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(request);
            return started.TryGetResult(out var jobId) ? jobId : JobId.NotSet;
        }
        finally
        {
            gate.Release();
        }
    }
}
