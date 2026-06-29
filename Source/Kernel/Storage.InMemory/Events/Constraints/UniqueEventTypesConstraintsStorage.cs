// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
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
        EventTypeId eventTypeId,
        EventSourceId eventSourceId,
        string scopeKey = "")
    {
        var existing = eventSequenceStorage.Events
            .FirstOrDefault(_ =>
                _.Context.EventType.Id == eventTypeId &&
                _.Context.EventSourceId == eventSourceId);

        if (existing is not null)
        {
            return Task.FromResult((false, existing.Context.SequenceNumber));
        }

        return Task.FromResult((true, EventSequenceNumber.Unavailable));
    }
}
