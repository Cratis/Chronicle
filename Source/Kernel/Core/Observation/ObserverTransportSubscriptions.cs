// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Looks up a transport subscription when the stored observer definition belongs to another event sequence.
/// </summary>
internal static class ObserverTransportSubscriptions
{
    /// <summary>
    /// Gets the subscription on the event sequence owning the stored observer definition, if different from the requested sequence.
    /// </summary>
    /// <param name="storage">The storage.</param>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="key">The requested observer key.</param>
    /// <returns>The owning sequence's subscription, or null when the requested sequence owns the definition.</returns>
    internal static async Task<ObserverSubscription?> OnOtherSequence(IStorage storage, IGrainFactory grainFactory, ObserverKey key)
    {
        var definition = await storage.GetEventStore(key.EventStore).Observers.Get(key.ObserverId);
        if (definition?.IsOwnedBy(key.EventSequenceId) != false)
        {
            return null;
        }

        var owningKey = key with { EventSequenceId = definition.EventSequenceId };
        return await grainFactory.GetGrain<IObserver>(owningKey).GetSubscription();
    }
}
