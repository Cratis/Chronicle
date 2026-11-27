// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Resolves an omitted observer sequence from its stored definition.
/// </summary>
internal static class ObserverEventSequenceResolver
{
    /// <summary>
    /// Resolve the sequence, preserving an explicit value and falling back to the log for missing definitions.
    /// </summary>
    /// <param name="storage">The storage containing observer definitions.</param>
    /// <param name="eventStore">The event store containing the observer.</param>
    /// <param name="observerId">The observer identifier.</param>
    /// <param name="eventSequenceId">The explicitly requested sequence, or an empty value.</param>
    /// <returns>The sequence to address.</returns>
    public static async Task<EventSequenceId> Resolve(IStorage storage, EventStoreName eventStore, ObserverId observerId, string eventSequenceId)
    {
        if (!string.IsNullOrEmpty(eventSequenceId))
        {
            return eventSequenceId;
        }

        var definition = await storage.GetEventStore(eventStore).Observers.Get(observerId);
        return Resolve(eventSequenceId, definition?.EventSequenceId);
    }

    /// <summary>
    /// Resolves the sequence when the stored definition has already been read.
    /// </summary>
    /// <param name="requested">The explicitly requested sequence, or an empty value.</param>
    /// <param name="stored">The sequence from the stored definition, if known.</param>
    /// <returns>The sequence to address.</returns>
    public static EventSequenceId Resolve(string requested, EventSequenceId? stored)
    {
        if (!string.IsNullOrEmpty(requested))
        {
            return requested;
        }

        return stored is null || stored == EventSequenceId.Unspecified || string.IsNullOrEmpty(stored.Value)
            ? EventSequenceId.Log
            : stored;
    }
}
