// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Narrows event sequence queries on the outbox to event types currently registered as public.
/// </summary>
/// <remarks>
/// Only the paged browse family (rows, count, histogram, export) uses this. Raw reads and subscription delivery are
/// unaffected. An event type counts as public when its latest registration is <see cref="EventTypeVisibility.Public"/>.
/// </remarks>
internal static class OutboxEventTypeQueryFilter
{
    /// <summary>
    /// Narrows the criteria for a sequence to public event types when the sequence is the outbox.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> holding the event types.</param>
    /// <param name="eventStore">The event store the sequence belongs to.</param>
    /// <param name="eventSequenceId">The event sequence being read.</param>
    /// <param name="criteria">The caller's <see cref="EventSequenceQueryCriteria"/>.</param>
    /// <returns>The criteria to read with, or <see langword="null"/> when nothing can match and storage must not be queried.</returns>
    internal static async Task<EventSequenceQueryCriteria?> Narrow(
        IStorage storage,
        string eventStore,
        string eventSequenceId,
        EventSequenceQueryCriteria criteria)
    {
        if (eventSequenceId != WellKnownEventSequences.Outbox)
        {
            return criteria;
        }

        var registrations = await storage.GetEventStore(eventStore).EventTypes.GetLatestForAllEventTypes();
        var publicTypes = registrations
            .Where(_ => _.Visibility == EventTypeVisibility.Public)
            .Select(_ => _.Type)
            .ToArray();

        var allowed = criteria.HasEventTypes
            ? criteria.EventTypes!.Where(requested => publicTypes.Any(_ => _.Id == requested.Id)).ToArray()
            : publicTypes;

        return allowed.Length == 0 ? null : criteria with { EventTypes = allowed };
    }
}
