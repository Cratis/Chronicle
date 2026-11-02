// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using Orleans.Placement;
using IEventSequence = KernelCore::Cratis.Chronicle.EventSequences.IEventSequence;
using IObserver = KernelCore::Cratis.Chronicle.Observation.IObserver;

namespace Cratis.Chronicle.Integration.Clustering.for_RolePlacement;

[GrainType("chronicle-integration-role-placement-caller")]
[PreferLocalPlacement]
public class PlacementCaller(ILocalSiloDetails localSiloDetails) : Grain, IPlacementCaller
{
    public async Task<SiloAddress> Activate(string observerKey, string eventSequenceKey)
    {
        // These calls run inside this silo's grain turn, not the test's external runtime context.
        await GrainFactory.GetGrain<IEventSequence>(eventSequenceKey).Rehydrate();
        await GrainFactory.GetGrain<IObserver>(observerKey).Ensure();
        return localSiloDetails.SiloAddress;
    }
}
