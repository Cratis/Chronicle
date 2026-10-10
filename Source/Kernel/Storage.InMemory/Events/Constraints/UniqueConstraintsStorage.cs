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
/// Per-event-source mode replaces the source's previous claim. Per-value mode retains each value separately,
/// with one owner per constraint and scope, until a removal event releases it.
/// </remarks>
public class UniqueConstraintsStorage : IUniqueConstraintsStorage
{
    readonly ConcurrentDictionary<(string EventSourceId, string ConstraintName, string ScopeKey), (string Value, EventSequenceNumber SequenceNumber)> _index = [];
    readonly ConcurrentDictionary<(string ConstraintName, string ScopeKey, string Value), (string EventSourceId, EventSequenceNumber SequenceNumber)> _values = [];

    /// <inheritdoc/>
    public Task ClearValues(UniqueConstraintDefinition definition, string scopeKey = "")
    {
        foreach (var entry in _values.Where(_ => _.Key.ConstraintName == definition.Name.Value && _.Key.ScopeKey == scopeKey))
        {
            _values.TryRemove(entry);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, string scopeKey = "")
    {
        if (definition.Mode == UniqueConstraintMode.PerValue)
        {
            return Task.FromResult(_values.TryGetValue((definition.Name.Value, scopeKey, value.Value), out var entry)
                ? (entry.EventSourceId == eventSourceId.Value, entry.SequenceNumber)
                : (true, EventSequenceNumber.Unavailable));
        }

        foreach (var (key, entry) in _index)
        {
            if (key.ConstraintName != definition.Name.Value || key.ScopeKey != scopeKey || entry.Value != value.Value)
            {
                continue;
            }

            return Task.FromResult((key.EventSourceId == eventSourceId.Value, entry.SequenceNumber));
        }

        return Task.FromResult((true, EventSequenceNumber.Unavailable));
    }

    /// <summary>
    /// Saves a claim using the definition's retention mode.
    /// </summary>
    /// <param name="eventSourceId">The owner.</param>
    /// <param name="definition">The constraint.</param>
    /// <param name="sequenceNumber">The claim's sequence number.</param>
    /// <param name="value">The hashed value.</param>
    /// <param name="scopeKey">The resolved scope.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="DuplicateUniqueConstraintValue">Another event source owns the value.</exception>
    public Task Save(EventSourceId eventSourceId, UniqueConstraintDefinition definition, EventSequenceNumber sequenceNumber, UniqueConstraintValue value, string scopeKey = "")
    {
        if (definition.Mode != UniqueConstraintMode.PerValue)
        {
            _index[(eventSourceId.Value, definition.Name.Value, scopeKey)] = (value.Value, sequenceNumber);
            return Task.CompletedTask;
        }

        var key = (definition.Name.Value, scopeKey, value.Value);
        var entry = _values.GetOrAdd(key, (eventSourceId.Value, sequenceNumber));
        if (entry.EventSourceId != eventSourceId.Value)
        {
            throw new DuplicateUniqueConstraintValue(definition.Name, eventSourceId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Releases every value held by the event source in the selected scope.
    /// </summary>
    /// <param name="eventSourceId">The owner.</param>
    /// <param name="definition">The constraint.</param>
    /// <param name="scopeKey">The resolved scope.</param>
    /// <returns>Awaitable task.</returns>
    public Task Remove(EventSourceId eventSourceId, UniqueConstraintDefinition definition, string scopeKey = "")
    {
        if (definition.Mode != UniqueConstraintMode.PerValue)
        {
            _index.TryRemove((eventSourceId.Value, definition.Name.Value, scopeKey), out _);
            return Task.CompletedTask;
        }

        foreach (var entry in _values.Where(_ => _.Key.ConstraintName == definition.Name.Value && _.Key.ScopeKey == scopeKey && _.Value.EventSourceId == eventSourceId.Value))
        {
            _values.TryRemove(entry);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Releases one value only if the source owns it.
    /// </summary>
    /// <param name="eventSourceId">The owner.</param>
    /// <param name="definition">The constraint.</param>
    /// <param name="value">The value to release.</param>
    /// <param name="scopeKey">The resolved scope.</param>
    /// <returns>Awaitable task.</returns>
    public Task RemoveValue(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, string scopeKey = "")
    {
        var key = (definition.Name.Value, scopeKey, value.Value);
        if (_values.TryGetValue(key, out var entry) && entry.EventSourceId == eventSourceId.Value)
        {
            _values.TryRemove(new KeyValuePair<(string, string, string), (string, EventSequenceNumber)>(key, entry));
        }

        return Task.CompletedTask;
    }
}
