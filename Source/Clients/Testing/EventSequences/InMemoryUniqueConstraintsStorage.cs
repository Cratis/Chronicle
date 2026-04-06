// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Chronicle.Storage.Events.Constraints;
using KernelConstraints = KernelConcepts::Cratis.Chronicle.Concepts.Events.Constraints;
using KernelEvents = KernelConcepts::Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents an in-memory implementation of <see cref="IUniqueConstraintsStorage"/> for testing.
/// </summary>
internal sealed class InMemoryUniqueConstraintsStorage : IUniqueConstraintsStorage
{
    // Key: (EventSourceId, ConstraintName, UniqueConstraintValue) → SequenceNumber
    readonly Dictionary<(string EventSourceId, string ConstraintName, string Value), KernelEvents::EventSequenceNumber> _index = [];

    /// <inheritdoc/>
    public Task<(bool IsAllowed, KernelEvents::EventSequenceNumber SequenceNumber)> IsAllowed(
        KernelEvents::EventSourceId eventSourceId,
        KernelConstraints::UniqueConstraintDefinition definition,
        KernelConstraints::UniqueConstraintValue value)
    {
        // Check if any OTHER event source has this value (unique across all sources)
        var conflict = _index
            .FirstOrDefault(kv =>
                kv.Key.ConstraintName == definition.Name.Value &&
                kv.Key.Value == value.Value &&
                kv.Key.EventSourceId != eventSourceId.Value);

        if (conflict.Key != default)
        {
            return Task.FromResult((false, conflict.Value));
        }

        return Task.FromResult((true, KernelEvents::EventSequenceNumber.Unavailable));
    }

    /// <inheritdoc/>
    public Task Save(
        KernelEvents::EventSourceId eventSourceId,
        KernelConstraints::ConstraintName name,
        KernelEvents::EventSequenceNumber sequenceNumber,
        KernelConstraints::UniqueConstraintValue value)
    {
        var key = (eventSourceId.Value, name.Value, value.Value);
        _index[key] = sequenceNumber;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Remove(
        KernelEvents::EventSourceId eventSourceId,
        KernelConstraints::ConstraintName name)
    {
        var keysToRemove = _index.Keys
            .Where(k => k.EventSourceId == eventSourceId.Value && k.ConstraintName == name.Value)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _index.Remove(key);
        }

        return Task.CompletedTask;
    }
}
