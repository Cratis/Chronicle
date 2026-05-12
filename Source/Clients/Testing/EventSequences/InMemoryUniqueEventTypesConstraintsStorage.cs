// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Chronicle.Storage.Events.Constraints;
using KernelEvents = KernelConcepts::Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents an in-memory implementation of <see cref="IUniqueEventTypesConstraintsStorage"/> for testing.
/// </summary>
/// <remarks>
/// Queries the associated <see cref="InMemoryEventSequenceStorage"/> to check whether an event of
/// the given type has already been appended for a specific event source — mirroring how the
/// MongoDB implementation reads the event sequence collection directly.
/// </remarks>
/// <param name="eventSequenceStorage">The <see cref="InMemoryEventSequenceStorage"/> to query.</param>
internal sealed class InMemoryUniqueEventTypesConstraintsStorage(
    InMemoryEventSequenceStorage eventSequenceStorage) : IUniqueEventTypesConstraintsStorage
{
    /// <inheritdoc/>
    public Task<(bool IsAllowed, KernelEvents::EventSequenceNumber SequenceNumber)> IsAllowed(
        KernelEvents::EventTypeId eventTypeId,
        KernelEvents::EventSourceId eventSourceId,
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

        return Task.FromResult((true, KernelEvents::EventSequenceNumber.Unavailable));
    }
}
