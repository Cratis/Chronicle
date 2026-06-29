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
public class UniqueConstraintsStorage : IUniqueConstraintsStorage
{
    /// <summary>
    /// Index keyed by (EventSourceId, ConstraintName, UniqueConstraintValue, ScopeKey) to its <see cref="EventSequenceNumber"/>.
    /// </summary>
    readonly ConcurrentDictionary<(string EventSourceId, string ConstraintName, string Value, string ScopeKey), EventSequenceNumber> _index = [];

    /// <inheritdoc/>
    public Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(
        EventSourceId eventSourceId,
        UniqueConstraintDefinition definition,
        UniqueConstraintValue value,
        string scopeKey = "")
    {
        // Check if any OTHER event source has this value (unique across all sources)
        var conflict = _index
            .FirstOrDefault(kv =>
                kv.Key.ConstraintName == definition.Name.Value &&
                kv.Key.Value == value.Value &&
                kv.Key.ScopeKey == scopeKey &&
                kv.Key.EventSourceId != eventSourceId.Value);

        if (conflict.Key != default)
        {
            return Task.FromResult((false, conflict.Value));
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
        var key = (eventSourceId.Value, name.Value, value.Value, scopeKey);
        _index[key] = sequenceNumber;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Remove(
        EventSourceId eventSourceId,
        ConstraintName name,
        string scopeKey = "")
    {
        var keysToRemove = _index.Keys
            .Where(k => k.EventSourceId == eventSourceId.Value && k.ConstraintName == name.Value && k.ScopeKey == scopeKey)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _index.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }
}
