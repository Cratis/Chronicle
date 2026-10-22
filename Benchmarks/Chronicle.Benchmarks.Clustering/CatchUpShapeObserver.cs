// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using Cratis.Chronicle.Jobs;
using Orleans;
using Observation = KernelCore::Cratis.Chronicle.Observation;
using Concepts = KernelConcepts::Cratis.Chronicle.Concepts.Observation;
using KernelProjections = KernelCore::Cratis.Chronicle.Projections;
using KernelReactors = KernelCore::Cratis.Chronicle.Observation.Reactors.Clients;

namespace Cratis.Chronicle.Benchmarks.Clustering;

internal sealed class CatchUpShapeObserver(Observation.IObserver observer, Observation.ObserverSubscription subscription)
{
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
        var observer = counters.Grains.GetGrain<Observation.IObserver>(new Concepts.ObserverKey(id, ClusterBenchmarkFixture.EventStore, KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName.Default, "event-log"));
        return new(observer, await observer.GetSubscription());
    }

    public Task Disconnect() => observer.Unsubscribe();

    public async Task VerifyBehind(ulong marker)
    {
        var state = await observer.GetState();
        if (await observer.IsSubscribed() || state.LastHandledEventSequenceNumber.Value != marker)
        {
            throw new InvalidOperationException("Observer did not remain disconnected at the marker while the backlog was seeded.");
        }
    }

    public Task Reconnect(string observerType) => observerType == "projection"
        ? Subscribe<KernelProjections.IProjectionObserverSubscriber>()
        : Subscribe<KernelReactors.IReactorObserverSubscriber>();

    public async Task WaitUntilCaughtUp(ClusterBenchmarkFixture fixture, ulong tail, TimeSpan timeout)
    {
        using var deadline = new CancellationTokenSource(timeout);
        while (!deadline.IsCancellationRequested)
        {
            var state = await observer.GetState().WaitAsync(deadline.Token);
            var jobs = (await fixture.EventStore1.Jobs.GetJobs().WaitAsync(deadline.Token)).ToArray();
            if (state.IsReplaying || state.FailedPartitionCount.Value > 0 || state.RunningState == Concepts.ObserverRunningState.Quarantined ||
                jobs.Any(job => job.Status is JobStatus.Failed or JobStatus.CompletedWithFailures or JobStatus.Stopped))
            {
                throw new InvalidOperationException("Observer replayed, quarantined, or failed during catch-up.");
            }

            var busy = jobs.Any(job => job.Status is JobStatus.PreparingJob or JobStatus.PreparingSteps or JobStatus.StartingSteps or JobStatus.Running or JobStatus.Removing);
            if (!busy && state.LastHandledEventSequenceNumber.IsActualValue && state.LastHandledEventSequenceNumber.Value >= tail && state.RunningState == Concepts.ObserverRunningState.Active &&
                state.CatchingUpPartitions.Count == 0 && state.ReplayingPartitions.Count == 0)
            {
                return;
            }

            await Task.Delay(50, deadline.Token);
        }

        throw new TimeoutException($"Observer did not catch up to {tail} within {timeout}.");
    }

    Task Subscribe<TSubscriber>()
        where TSubscriber : Observation.IObserverSubscriber => observer.SubscribeWithGenerationDelivery<TSubscriber>(
            subscription.SubscriberType == typeof(KernelProjections.IProjectionObserverSubscriber) ? Concepts.ObserverType.Projection : Concepts.ObserverType.Reactor,
            subscription.EventTypes,
            subscription.SiloAddress,
            subscription.GenerationDelivery,
            subscription.Arguments,
            subscription.IsReplayable,
            subscription.Filters);
}
