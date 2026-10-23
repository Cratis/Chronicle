// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using Cratis.Chronicle.Jobs;
using KernelObservation = KernelCore::Cratis.Chronicle.Observation;
using KernelObserverConcepts = KernelConcepts::Cratis.Chronicle.Concepts.Observation;
using KernelProjections = KernelCore::Cratis.Chronicle.Projections;
using KernelReactors = KernelCore::Cratis.Chronicle.Observation.Reactors.Clients;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Controls and inspects the observer used in the catch-up benchmark.
/// </summary>
/// <param name="observer">The kernel observer.</param>
/// <param name="subscription">The original subscription.</param>
internal sealed class CatchUpShapeObserver(KernelObservation.IObserver observer, KernelObservation.ObserverSubscription subscription)
{
    /// <summary>
    /// Registers the observer and resolves its grain.
    /// </summary>
    /// <param name="fixture">The cluster fixture.</param>
    /// <param name="counters">The counters.</param>
    /// <param name="settings">The benchmark settings.</param>
    /// <returns>The observer wrapper.</returns>
    public static async Task<CatchUpShapeObserver> Register(ClusterBenchmarkFixture fixture, CatchUpShapeCounters counters, CatchUpShapeSettings settings)
    {
        string id;
        if (settings.ObserverType == "projection")
        {
            var handler = await fixture.EventStore1.Projections.Register<CatchUpShapeReadModel>(builder => builder.From<CatchUpShapeValueSet>());
            id = handler.Id.Value;
        }
        else
        {
            var handler = await fixture.EventStore1.Reactors.Register("catchup-shape-reactor", builder => builder.On<CatchUpShapeValueSet>(_ => { }));
            id = handler.Id.Value;
        }

        await fixture.Readiness.WaitForSubscribedObserver(id, TimeSpan.FromSeconds(settings.TimeoutSeconds));
        counters.ExpectObserver(id);
        var observer = counters.Grains.GetGrain<KernelObservation.IObserver>(new KernelObserverConcepts.ObserverKey(id, ClusterBenchmarkFixture.EventStore, KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName.Default, "event-log"));
        return new(observer, await observer.GetSubscription());
    }

    /// <summary>
    /// Unsubscribes the observer.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task Disconnect() => observer.Unsubscribe();

    /// <summary>
    /// Verifies the observer is disconnected at the marker.
    /// </summary>
    /// <param name="marker">The expected last handled sequence number.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the observer did not stay at the marker.</exception>
    public async Task VerifyBehind(ulong marker)
    {
        var state = await observer.GetState();
        if (await observer.IsSubscribed() || state.LastHandledEventSequenceNumber.Value != marker)
        {
            throw new InvalidOperationException("Observer did not remain disconnected at the marker while the backlog was seeded.");
        }
    }

    /// <summary>
    /// Resubscribes the observer.
    /// </summary>
    /// <param name="observerType">The observer type, projection or reactor.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task Reconnect(string observerType)
    {
        if (observerType != "projection")
        {
            return Subscribe<KernelReactors.IReactorObserverSubscriber>(KernelObserverConcepts.ObserverType.Reactor);
        }

        if (subscription.SubscriberType == typeof(KernelProjections.ICollapsingProjectionObserverSubscriber))
        {
            return Subscribe<KernelProjections.ICollapsingProjectionObserverSubscriber>(KernelObserverConcepts.ObserverType.Projection);
        }

        return Subscribe<KernelProjections.IProjectionObserverSubscriber>(KernelObserverConcepts.ObserverType.Projection);
    }

    /// <summary>
    /// Prints the subscriber and observer types used for the measurement.
    /// </summary>
    /// <param name="observerType">The observer type, projection or reactor.</param>
    public void PrintSubscription(string observerType) => Console.WriteLine(
        $"SubscriberType={subscription.SubscriberType.FullName}, ObserverType={(observerType == "projection" ? KernelObserverConcepts.ObserverType.Projection : KernelObserverConcepts.ObserverType.Reactor)}");

    /// <summary>
    /// Waits until the observer has handled the tail.
    /// </summary>
    /// <param name="fixture">The cluster fixture.</param>
    /// <param name="tail">The tail sequence number.</param>
    /// <param name="timeout">The maximum time to wait.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the observer replayed, was quarantined or failed.</exception>
    /// <exception cref="TimeoutException">Thrown when the observer did not catch up in time.</exception>
    public async Task WaitUntilCaughtUp(ClusterBenchmarkFixture fixture, ulong tail, TimeSpan timeout)
    {
        using var deadline = new CancellationTokenSource(timeout);
        while (!deadline.IsCancellationRequested)
        {
            var state = await observer.GetState().WaitAsync(deadline.Token);
            var jobs = (await fixture.EventStore1.Jobs.GetJobs().WaitAsync(deadline.Token)).ToArray();
            if (state.IsReplaying || state.FailedPartitionCount.Value > 0 || state.RunningState == KernelObserverConcepts.ObserverRunningState.Quarantined ||
                jobs.Any(job => job.Status is JobStatus.Failed or JobStatus.CompletedWithFailures or JobStatus.Stopped))
            {
                throw new InvalidOperationException("Observer replayed, quarantined, or failed during catch-up.");
            }

            var busy = jobs.Any(job => job.Status is JobStatus.PreparingJob or JobStatus.PreparingSteps or JobStatus.StartingSteps or JobStatus.Running or JobStatus.Removing);
            if (!busy && state.LastHandledEventSequenceNumber.IsActualValue && state.LastHandledEventSequenceNumber.Value >= tail && state.RunningState == KernelObserverConcepts.ObserverRunningState.Active &&
                state.CatchingUpPartitions.Count == 0 && state.ReplayingPartitions.Count == 0)
            {
                return;
            }

            await Task.Delay(50, deadline.Token);
        }

        throw new TimeoutException($"Observer did not catch up to {tail} within {timeout}.");
    }

    Task Subscribe<TSubscriber>(KernelObserverConcepts.ObserverType observerType)
        where TSubscriber : KernelObservation.IObserverSubscriber => observer.SubscribeWithGenerationDelivery<TSubscriber>(
            observerType,
            subscription.EventTypes,
            subscription.SiloAddress,
            subscription.GenerationDelivery,
            subscription.Arguments,
            subscription.IsReplayable,
            subscription.Filters);
}
