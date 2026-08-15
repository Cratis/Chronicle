// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.InMemory.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints;

/// <summary>
/// Represents an in-memory implementation of <see cref="IUniqueEventTypesConstraintsStorage"/>.
/// </summary>
/// <remarks>
/// Queries the associated <see cref="EventSequenceStorage"/> to check whether an event of
/// the given type has already been appended for a specific event source - mirroring how the
/// MongoDB implementation reads the event sequence collection directly.
/// </remarks>
/// <param name="eventSequenceStorage">The <see cref="EventSequenceStorage"/> to query.</param>
public class UniqueEventTypesConstraintsStorage(
    EventSequenceStorage eventSequenceStorage) : IUniqueEventTypesConstraintsStorage
{
    /// <inheritdoc/>
    public Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(
        UniqueEventTypeConstraintDefinition definition,
        EventSourceId eventSourceId,
        string scopeKey = "")
    {
        var coveredEventTypeIds = definition.EventTypeIds.ToHashSet();
        var forEventSource = eventSequenceStorage.Events
            .Where(_ => _.Context.EventSourceId == eventSourceId)
            .ToArray();

        var latestRemoval = GetLatestRemoval(definition, forEventSource);

        // Ordered so the sequence number reported back is the covered event that actually holds the cycle, rather
        // than whichever one the sequence happened to yield first.
        var existing = forEventSource
            .Where(_ => coveredEventTypeIds.Contains(_.Context.EventType.Id) &&
                        (latestRemoval is null || _.Context.SequenceNumber.Value > latestRemoval.Value))
            .OrderBy(_ => _.Context.SequenceNumber.Value)
            .FirstOrDefault();

        if (existing is not null)
        {
            return Task.FromResult((false, existing.Context.SequenceNumber));
        }

        return Task.FromResult((true, EventSequenceNumber.Unavailable));
    }

    /// <summary>
    /// Find the most recent event on the event source that releases the constraint.
    /// </summary>
    /// <param name="definition">The <see cref="UniqueEventTypeConstraintDefinition"/> to read the removal events from.</param>
    /// <param name="forEventSource">The events already appended for the event source being answered for.</param>
    /// <returns>The <see cref="EventSequenceNumber"/> the current cycle starts after, or <see langword="null"/> when nothing released it.</returns>
    /// <remarks>
    /// Any of the declared removal events ends a cycle, so the latest across all of them is the one that counts —
    /// looking at only one of them would keep answering against a cycle that another terminal fact already closed.
    /// </remarks>
    static EventSequenceNumber? GetLatestRemoval(UniqueEventTypeConstraintDefinition definition, IEnumerable<AppendedEvent> forEventSource)
    {
        var removalEventTypeIds = definition.RemovedWith.ToHashSet();
        if (removalEventTypeIds.Count == 0)
        {
            return null;
        }

        var removals = forEventSource
            .Where(_ => removalEventTypeIds.Contains(_.Context.EventType.Id))
            .Select(_ => _.Context.SequenceNumber.Value)
            .ToArray();

        return removals.Length == 0 ? null : removals.Max();
    }
}
