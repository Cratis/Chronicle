// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Releases events to the generations selected by a pinned projection.
/// </summary>
internal static class PinnedGenerationRelease
{
    /// <summary>
    /// Releases every event of a pinned type, including revisions and protected values, preserving their order.
    /// </summary>
    /// <param name="projection">The projection selecting the generations.</param>
    /// <param name="events">The stored events to release.</param>
    /// <returns>The events selected for the projection.</returns>
    internal static async Task<AppendedEvent[]> ReleaseToPins(this IProjection projection, IEnumerable<AppendedEvent> events)
    {
        var released = events.ToArray();
        if (projection.GenerationDelivery != EventGenerationDelivery.Pinned)
        {
            return released;
        }

        var pinsById = projection.EventTypes.DistinctBy(_ => _.Id).ToDictionary(_ => _.Id);
        for (var index = 0; index < released.Length; index++)
        {
            var @event = released[index];
            if (pinsById.TryGetValue(@event.Context.EventType.Id, out var pin))
            {
                released[index] = await projection.ReleaseEvent(pin, @event);
            }
        }

        return released;
    }
}
