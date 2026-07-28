// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints;

/// <summary>
/// Represents an in-memory implementation of <see cref="IUniqueConstraintsStorage"/>.
/// </summary>
/// <remarks>
/// The index holds at most one entry per event source for a given constraint and scope, mirroring the
/// document-per-event-source shape of the MongoDB and SQL implementations. Saving a new value for an event source
/// therefore replaces its previous claim and releases the value it held, making that value claimable by others.
/// </remarks>
public class UniqueConstraintsStorage : IUniqueConstraintsStorage
{
    /// <summary>
    /// Index keyed by (EventSourceId, ConstraintName, ScopeKey) to the claimed value and its <see cref="EventSequenceNumber"/>.
    /// </summary>
    readonly ConcurrentDictionary<(string EventSourceId, string ConstraintName, string ScopeKey), (string Value, EventSequenceNumber SequenceNumber)> _index = [];

    /// <inheritdoc/>
    public Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(
        EventSourceId eventSourceId,
        UniqueConstraintDefinition definition,
        UniqueConstraintValue value,
        string scopeKey = "")
    {
        // Note: Case-insensitive comparison is handled by hashing the value with case normalization
        // before it reaches the storage layer, so we can use a simple equality check here.
        foreach (var (key, entry) in _index)
        {
            if (key.ConstraintName != definition.Name.Value ||
                key.ScopeKey != scopeKey ||
                entry.Value != value.Value)
            {
                continue;
            }

            return Task.FromResult((key.EventSourceId == eventSourceId.Value, entry.SequenceNumber));
        }

        return Task.FromResult((true, EventSequenceNumber.Unavailable));
    }

    /// <inheritdoc/>
    public Task Save(
        EventSourceId eventSourceId,
        ConstraintName name,
        EventSequenceNumber sequenceNumber,
        UniqueConstraintValue value,
        string scopeKey = "")
    {
        _index[(eventSourceId.Value, name.Value, scopeKey)] = (value.Value, sequenceNumber);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Remove(
        EventSourceId eventSourceId,
        ConstraintName name,
        string scopeKey = "")
    {
        _index.TryRemove((eventSourceId.Value, name.Value, scopeKey), out _);
        return Task.CompletedTask;
    }
}
