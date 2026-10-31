// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Orleans.Core.Internal;

namespace Cratis.Chronicle.DevelopmentTools;

/// <summary>
/// Stops live delivery and explicitly deactivates KeepAlive grains before development or integration storage is wiped.
/// </summary>
/// <remarks>Not a production lifecycle operation. Callers must prevent new client work for the duration of the reset.</remarks>
public static class KeepAliveGrainReset
{
    /// <summary>
    /// Resets all active appended-events queues, routing grains and observers across the cluster.
    /// </summary>
    /// <param name="grainFactory">The grain factory for the kernel being reset.</param>
    /// <returns>A task representing completion of the reset.</returns>
    public static async Task Reset(IGrainFactory grainFactory)
    {
        var management = grainFactory.GetGrain<IManagementGrain>(0);
        var activations = await management.GetDetailedGrainStatistics();
        var queueType = grainFactory.GetGrain<IAppendedEventsQueue>(0, "reset-type-lookup").GetGrainId().Type;
        var queuesType = grainFactory.GetGrain<IAppendedEventsQueues>("reset-type-lookup").GetGrainId().Type;
        var observerType = grainFactory.GetGrain<IObserver>("reset-type-lookup").GetGrainId().Type;
        var grainIds = activations.Select(activation => activation.GrainId).Distinct().ToArray();
        var queues = grainIds.Where(id => id.Type == queueType).ToArray();
        var routers = grainIds.Where(id => id.Type == queuesType).ToArray();
        var observers = grainIds.Where(id => id.Type == observerType).ToArray();

        // Individual queues can outlive their router. Reset them from the activation inventory, not only through routers.
        await Task.WhenAll(queues.Select(id => grainFactory.GetGrain<IAppendedEventsQueue>(id).ResetForDevelopment()));
        await Task.WhenAll(routers.Select(id => grainFactory.GetGrain<IAppendedEventsQueues>(id).ResetForDevelopment()));
        await Task.WhenAll(observers.Select(id => grainFactory.GetGrain<IObserverDevelopmentReset>(id).ResetForDevelopment()));
        await Task.WhenAll(queues.Concat(routers).Select(async id =>
            await grainFactory.GetGrain<IGrainManagementExtension>(id).DeactivateOnIdle()));
    }
}
