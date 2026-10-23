// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using Microsoft.Extensions.Options;
using Configuration = KernelCore::Cratis.Chronicle.Configuration;
using EventSequenceKey = KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceKey;
using IEventSequence = KernelCore::Cratis.Chronicle.EventSequences.IEventSequence;
using IObserver = KernelCore::Cratis.Chronicle.Observation.IObserver;
using ISiloRoles = KernelCore::Cratis.Chronicle.Cluster.ISiloRoles;
using ObserverKey = KernelConcepts::Cratis.Chronicle.Concepts.Observation.ObserverKey;

namespace Cratis.Chronicle.Integration.Clustering.for_RolePlacement;

public static class PlacementScenario
{
    public static async Task<GrainPlacement[]> ActivateFromBothSilos(ClusteringFixture fixture)
    {
        await WaitForRoleMetadata(fixture);
        var first = await Activate(fixture.SiloServices);
        var second = await Activate(fixture.SecondSiloServices);
        var management = fixture.SiloServices.GetRequiredService<IGrainFactory>().GetGrain<IManagementGrain>(0);
        var statistics = await management.GetDetailedGrainStatistics();

        // Exact grain ids exclude warmup activations. Single also fails for missing or duplicate activations.
        return [Observe(first), Observe(second)];

        GrainPlacement Observe((SiloAddress Caller, GrainId Observer, GrainId EventSequence) activation) => new(
            activation.Caller,
            statistics.Single(statistic => statistic.GrainId == activation.Observer).SiloAddress,
            statistics.Single(statistic => statistic.GrainId == activation.EventSequence).SiloAddress);
    }

    static async Task<(SiloAddress Caller, GrainId Observer, GrainId EventSequence)> Activate(IServiceProvider services)
    {
        var grainFactory = services.GetRequiredService<IGrainFactory>();
        var id = Guid.NewGuid().ToString();
        var sequenceKey = new EventSequenceKey(
            $"placement-{id}",
            Constants.EventStore,
            KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName.Default);
        var observerKey = new ObserverKey(id, sequenceKey.EventStore, sequenceKey.Namespace, sequenceKey.EventSequenceId);
        var observer = grainFactory.GetGrain<IObserver>(observerKey);
        var sequence = grainFactory.GetGrain<IEventSequence>(sequenceKey);
        var caller = grainFactory.GetGrain<IPlacementCaller>(Guid.NewGuid());
        var callerAddress = await caller.Activate(observerKey, sequenceKey).WaitAsync(TimeSpan.FromSeconds(60));
        return (callerAddress, observer.GetGrainId(), sequence.GetGrainId());
    }

    static async Task WaitForRoleMetadata(ClusteringFixture fixture)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        // Membership convergence alone does not mean the remote role metadata cache has caught up.
        while (!HasExpectedRoles(fixture.SiloServices, fixture.SecondSiloServices, fixture.ObserversSiloAddress) ||
               !HasExpectedRoles(fixture.SecondSiloServices, fixture.SiloServices, fixture.EventSequencesSiloAddress))
        {
            await Task.Delay(100, timeout.Token);
        }
    }

    static bool HasExpectedRoles(IServiceProvider caller, IServiceProvider target, SiloAddress address)
    {
        var expected = target.GetRequiredService<IOptions<Configuration.ChronicleOptions>>().Value.Clustering.Roles;
        return caller.GetRequiredService<ISiloRoles>().GetRoles(address).TryGetValue(out var roles) &&
               roles.EventSequences == expected.EventSequences &&
               roles.Observers == expected.Observers &&
               roles.Jobs == expected.Jobs;
    }
}
