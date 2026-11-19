// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// Removes subscription ownership before a development reset wipes storage.
/// </summary>
/// <remarks>Callers must stop delivery and prevent new client work for the duration of the reset.</remarks>
internal static class EventStoreSubscriptionsReset
{
    /// <summary>
    /// Removes subscriptions from every stored or active subscription owner before storage is wiped.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="storage">The event store inventory.</param>
    /// <returns>A task that completes when all subscriptions have been removed.</returns>
    internal static async Task Reset(IGrainFactory grainFactory, IStorage storage)
    {
        var management = grainFactory.GetGrain<IManagementGrain>(0);
        var activations = await management.GetDetailedGrainStatistics();
        var managerType = grainFactory.GetGrain<IEventStoreSubscriptionsManager>("reset-type-lookup").GetGrainId().Type;
        var eventStores = (await storage.GetEventStores()).Select(store => store.Value)
            .Concat(activations.Where(activation => activation.GrainId.Type == managerType).Select(activation => activation.GrainId.Key.ToString()))
            .Distinct();

        foreach (var eventStore in eventStores)
        {
            var manager = grainFactory.GetGrain<IEventStoreSubscriptionsManager>(eventStore);

            // Remove clears the live definition and pending revocations, unregisters its reminder, and unsubscribes
            // its source observers. Include dormant owners from storage, not just those in the activation inventory.
            var definitions = await manager.GetSubscriptionDefinitions();
            foreach (var definition in definitions.ToArray())
            {
                await manager.Remove(definition.Identifier);
            }
        }
    }
}
#endif
