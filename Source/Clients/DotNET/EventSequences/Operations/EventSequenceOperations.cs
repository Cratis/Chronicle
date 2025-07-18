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
    Causation? _causation;

    /// <inheritdoc/>
    public IEventSequence EventSequence => eventSequence;

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
    public IEnumerable<object> GetAppendedEvents() =>
        _eventSourceBuilders.Values
            .SelectMany(builder => builder.GetAppendedEvents())
            .ToArray();

    /// <inheritdoc/>
    public void Clear()
    {
        _eventSourceBuilders.Clear();
        _causation = null;
    }

    /// <inheritdoc/>
    public Task<AppendManyResult> Perform()
    {
        var events = new List<EventForEventSourceId>();
        var concurrencyScopes = _eventSourceBuilders.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ConcurrencyScope);

        foreach (var (eventSourceId, operations) in _eventSourceBuilders)
        {
            var appendOperations = operations.GetOperationsOfType<AppendOperation>();
            if (appendOperations.Any())
            {
                events.AddRange(appendOperations.Select(op => new EventForEventSourceId(eventSourceId, op.Event, op.Causation ?? _causation ?? Causation.Unknown())
                {
                    EventStreamType = op.EventStreamType ?? EventStreamType.All,
                    EventStreamId = op.EventStreamId ?? EventStreamId.Default,
                    EventSourceType = op.EventSourceType ?? EventSourceType.Default
                }));
            }
        }
        return eventSequence.AppendMany(events, concurrencyScopes: concurrencyScopes);
    }
}
