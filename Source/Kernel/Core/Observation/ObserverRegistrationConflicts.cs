// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Recommendations;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Reports cross-sequence registration conflicts in each affected namespace.
/// </summary>
internal static class ObserverRegistrationConflicts
{
    /// <summary>
    /// Adds a registration conflict recommendation to each namespace of the event store.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get the recommendations managers from.</param>
    /// <param name="eventStore">The event store the observer belongs to.</param>
    /// <param name="namespaces">The namespaces to report in.</param>
    /// <param name="request">The conflict to report.</param>
    /// <returns>Awaitable task.</returns>
    internal static async Task Report(IGrainFactory grainFactory, EventStoreName eventStore, IEnumerable<EventStoreNamespaceName> namespaces, ObserverRegistrationConflictRequest request)
    {
        foreach (var @namespace in namespaces)
        {
            var manager = grainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(eventStore, @namespace));
            await manager.Add<IObserverRegistrationConflictRecommendation, ObserverRegistrationConflictRequest>(
                $"Observer '{request.ObserverId}' registration on '{request.IncomingEventSequenceId}' from client '{request.IncomingVersion}' conflicts with owner '{request.OwnerEventSequenceId}' from client '{request.OwnerVersion}' ({request.Outcome}): deploy the newer version everywhere, or set Cratis:Chronicle:ApplicationVersion / Cratis__Chronicle__ApplicationVersion to a higher, monotonic version.",
                request);
        }
    }
}
