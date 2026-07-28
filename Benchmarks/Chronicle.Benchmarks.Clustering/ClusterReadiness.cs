// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using Cratis.Chronicle.Jobs;
using IKernelObserver = KernelCore::Cratis.Chronicle.Observation.IObserver;
using KernelEventSequenceId = KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId;
using KernelEventStoreName = KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName;
using KernelEventStoreNamespaceName = KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName;
using ObserverKey = KernelConcepts::Cratis.Chronicle.Concepts.Observation.ObserverKey;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Probes whether a running cluster is in a state where a measurement means anything.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> of a deployed silo.</param>
/// <param name="eventStore">The <see cref="IEventStore"/> the benchmarks work against.</param>
/// <param name="eventStoreName">The name of that event store.</param>
public sealed class ClusterReadiness(IGrainFactory grainFactory, IEventStore eventStore, string eventStoreName)
{
    /// <summary>
    /// Waits until the kernel observer with the given identifier is subscribed to at least one event type,
    /// and throws when it never gets there.
    /// </summary>
    /// <remarks>
    /// An observer that is not subscribed never does any work, so a wait for it to reach a sequence number
    /// would either return immediately or time out — either way the benchmark would be measuring the append
    /// alone. This makes that state fail loudly before the measured window opens. The subscription's target
    /// list is deliberately not checked: it only holds connected client instances, and kernel-owned
    /// subscriptions such as projections legitimately have none.
    /// </remarks>
    /// <param name="observerId">The identifier of the observer.</param>
    /// <param name="timeout">The maximum time to wait.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the observer never becomes subscribed.</exception>
    public async Task WaitForSubscribedObserver(string observerId, TimeSpan timeout)
    {
        var observer = grainFactory.GetGrain<IKernelObserver>(new ObserverKey(
            observerId,
            (KernelEventStoreName)eventStoreName,
            KernelEventStoreNamespaceName.Default,
            KernelEventSequenceId.Log));

        using var cancellationTokenSource = new CancellationTokenSource(timeout);
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var subscription = await observer.GetSubscription();
            if (subscription.IsSubscribed && subscription.EventTypes.Any())
            {
                return;
            }

            await Task.Delay(200, cancellationTokenSource.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        throw new InvalidOperationException(
            $"Observer '{observerId}' never became subscribed to any event type. Measuring it would not include any observer work.");
    }

    /// <summary>
    /// Waits until no job in the event store is preparing or running.
    /// </summary>
    /// <remarks>
    /// Observer work is driven by jobs, so this is the cluster's "nothing in flight" signal. Used both to
    /// open a measured window from a quiescent cluster and to close one only once the work it triggered —
    /// including any catch-up a replay leaves behind — has actually finished.
    /// </remarks>
    /// <param name="timeout">The maximum time to wait.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when jobs are still in flight after the timeout.</exception>
    public async Task WaitForNoJobsInFlight(TimeSpan timeout)
    {
        using var cancellationTokenSource = new CancellationTokenSource(timeout);
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var jobs = await eventStore.Jobs.GetJobs();
            if (!jobs.Any(job => job.Status is JobStatus.PreparingJob or JobStatus.PreparingSteps or JobStatus.StartingSteps or JobStatus.Running or JobStatus.Removing))
            {
                return;
            }

            await Task.Delay(50, cancellationTokenSource.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        throw new InvalidOperationException("Jobs were still in flight after the timeout.");
    }
}
