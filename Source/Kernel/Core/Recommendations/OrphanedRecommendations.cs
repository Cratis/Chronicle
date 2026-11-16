// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Recommendations;

/// <summary>
/// Removes old recommendations whose subjects no longer exist.
/// </summary>
internal static class OrphanedRecommendations
{
    /// <summary>
    /// Prunes recommendations without activating recommendation grains.
    /// </summary>
    /// <param name="eventStore">The event store storage.</param>
    /// <param name="namespace">The namespace storage.</param>
    /// <param name="cutoff">The exclusive age cutoff protecting registrations in progress.</param>
    /// <returns>The number of rows removed.</returns>
    internal static async Task<int> Prune(IEventStoreStorage eventStore, IEventStoreNamespaceStorage @namespace, DateTimeOffset cutoff)
    {
        var recommendations = await @namespace.Recommendations.GetAll();
        var subjects = recommendations.Where(row => row.Occurred < cutoff).GroupBy(IgnoredRecommendations.Subject);
        var count = 0;
        foreach (var rows in subjects)
        {
            if (rows.Key is { } subject && !await Exists(eventStore, @namespace, subject))
            {
                count += await IgnoredRecommendations.Remove(@namespace.Recommendations, rows);
            }
        }
        return count;
    }

    static async Task<bool> Exists(IEventStoreStorage eventStore, IEventStoreNamespaceStorage @namespace, ObserverId subject) =>
        await eventStore.Observers.Has(subject) ||
        await eventStore.Projections.Has((ProjectionId)subject.Value) ||
        (await @namespace.Observers.Get(subject)).Identifier == subject ||
        await eventStore.Reactors.Has((ReactorId)subject.Value) ||
        await eventStore.Reducers.Has((ReducerId)subject.Value) ||
        await eventStore.Webhooks.Has((WebhookId)subject.Value);
}
