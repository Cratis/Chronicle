// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints;

/// <summary>
/// Represents an in-memory implementation of <see cref="IUniqueConstraintsStorage"/>.
/// </summary>
/// <remarks>
/// Each value has one owner per constraint and scope. Per-event-source mode replaces the source's previous claim
/// only after the new value is available. Per-value mode retains each value until a removal event releases it.
/// </remarks>
public class UniqueConstraintsStorage : IUniqueConstraintsStorage
{
    readonly object _lock = new();
    readonly Dictionary<(string EventSourceId, string ConstraintName, string ScopeKey), (string Value, EventSequenceNumber SequenceNumber)> _index = [];
    readonly Dictionary<(string ConstraintName, string ScopeKey, string Value), (string EventSourceId, EventSequenceNumber SequenceNumber)> _values = [];

    /// <inheritdoc/>
    public Task ClearValues(UniqueConstraintDefinition definition, string scopeKey = "", CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            foreach (var key in _index.Keys.Where(_ => _.ConstraintName == definition.Name.Value && _.ScopeKey == scopeKey).ToArray())
            {
                _index.Remove(key);
            }

            foreach (var key in _values.Keys.Where(_ => _.ConstraintName == definition.Name.Value && _.ScopeKey == scopeKey).ToArray())
            {
                _values.Remove(key);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, string scopeKey = "", CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult(_values.TryGetValue((definition.Name.Value, scopeKey, value.Value), out var entry)
                ? (entry.EventSourceId == eventSourceId.Value, entry.SequenceNumber)
                : (true, EventSequenceNumber.Unavailable));
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<UniqueConstraintValue>> GetFor(EventSourceId eventSourceId, UniqueConstraintDefinition definition, string scopeKey = "", CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult(_values.Where(entry => entry.Key.ConstraintName == definition.Name.Value && entry.Key.ScopeKey == scopeKey && entry.Value.EventSourceId == eventSourceId.Value)
                .Select(entry => new UniqueConstraintValue(entry.Key.Value)).ToArray().AsEnumerable());
        }
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<UniqueConstraintValue> GetForStream(EventSourceId eventSourceId, UniqueConstraintDefinition definition, string scopeKey = "", CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var values = _values.Where(entry => entry.Key.ConstraintName == definition.Name.Value && entry.Key.ScopeKey == scopeKey && entry.Value.EventSourceId == eventSourceId.Value)
                .Select(entry => new UniqueConstraintValue(entry.Key.Value)).ToArray();
            return new UniqueConstraintValues(values, cancellationToken);
        }
    }

    /// <summary>
    /// Saves a claim using the definition's retention mode.
    /// </summary>
    /// <param name="eventSourceId">The owner.</param>
    /// <param name="definition">The constraint.</param>
    /// <param name="sequenceNumber">The claim's sequence number.</param>
    /// <param name="value">The hashed value.</param>
    /// <param name="scopeKey">The resolved scope.</param>
    /// <param name="cancellationToken">Cancellation of the write.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="DuplicateUniqueConstraintValue">Another event source owns the value.</exception>
    public Task Save(EventSourceId eventSourceId, UniqueConstraintDefinition definition, EventSequenceNumber sequenceNumber, UniqueConstraintValue value, string scopeKey = "", CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var key = (definition.Name.Value, scopeKey, value.Value);
            if (_values.TryGetValue(key, out var owner))
            {
                if (owner.EventSourceId != eventSourceId.Value)
                {
                    throw new DuplicateUniqueConstraintValue(definition.Name, eventSourceId);
                }

                return Task.CompletedTask;
            }

            if (definition.Mode != UniqueConstraintMode.PerValue)
            {
                var sourceKey = (eventSourceId.Value, definition.Name.Value, scopeKey);
                if (_index.TryGetValue(sourceKey, out var previous))
                {
                    _values.Remove((definition.Name.Value, scopeKey, previous.Value));
                }

                _index[sourceKey] = (value.Value, sequenceNumber);
            }

            _values[key] = (eventSourceId.Value, sequenceNumber);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Releases every value held by the event source in the selected scope.
    /// </summary>
    /// <param name="eventSourceId">The owner.</param>
    /// <param name="definition">The constraint.</param>
    /// <param name="scopeKey">The resolved scope.</param>
    /// <param name="cancellationToken">Cancellation of the write.</param>
    /// <returns>Awaitable task.</returns>
    public Task Remove(EventSourceId eventSourceId, UniqueConstraintDefinition definition, string scopeKey = "", CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            if (definition.Mode != UniqueConstraintMode.PerValue)
            {
                if (_index.Remove((eventSourceId.Value, definition.Name.Value, scopeKey), out var previous))
                {
                    _values.Remove((definition.Name.Value, scopeKey, previous.Value));
                }
            }
            else
            {
                foreach (var key in _values.Where(_ => _.Key.ConstraintName == definition.Name.Value && _.Key.ScopeKey == scopeKey && _.Value.EventSourceId == eventSourceId.Value).Select(_ => _.Key).ToArray())
                {
                    _values.Remove(key);
                }
            }
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
    /// <param name="cancellationToken">Cancellation of the write.</param>
    /// <returns>Awaitable task.</returns>
    public Task RemoveValue(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, string scopeKey = "", CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var key = (definition.Name.Value, scopeKey, value.Value);
            if (definition.Mode == UniqueConstraintMode.PerValue && _values.TryGetValue(key, out var entry) && entry.EventSourceId == eventSourceId.Value)
            {
                _values.Remove(key);
            }
        }

        return Task.CompletedTask;
    }
}
