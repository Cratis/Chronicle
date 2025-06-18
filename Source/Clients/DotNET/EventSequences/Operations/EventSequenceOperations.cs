// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Operations;

/// <summary>
/// Represents an implementation of <see cref="IEventSequenceOperations"/> for managing operations related to an event sequence.
/// </summary>
/// <param name="eventSequence">The event sequence to operate on.</param>
public class EventSequenceOperations(IEventSequence eventSequence) : IEventSequenceOperations
{
    readonly Dictionary<EventSourceId, EventSourceOperations> _eventSourceBuilders = [];
    bool _isTransactional;
    Causation? _causation;

    /// <inheritdoc/>
    public EventSequenceOperations ForEventSourceId(
        EventSourceId eventSourceId,
        Action<EventSourceOperations> configure)
    {
        if (!_eventSourceBuilders.TryGetValue(eventSourceId, out var operations))
        {
            operations = new EventSourceOperations();
            _eventSourceBuilders[eventSourceId] = operations;
        }
        configure(operations);
        _eventSourceBuilders[eventSourceId] = operations;
        return this;
    }

    /// <inheritdoc/>
    public EventSequenceOperations WithCausation(Causation causation)
    {
        _causation = causation;
        return this;
    }

    /// <inheritdoc/>
    public EventSequenceOperations Transactional()
    {
        _isTransactional = true;
        return this;
    }

    /// <inheritdoc/>
    public AppendManyResult Perform()
    {
        if (_isTransactional)
        {
        }

        // Convert to append many operation
        return new AppendManyResult();
    }
}
