// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Builder for creating a <see cref="ConcurrencyScope"/> for an event sequence append operation.
/// </summary>
public class ConcurrencyScopeBuilder
{
    readonly List<EventType> _eventTypes = [];
    EventSequenceNumber _sequenceNumber = EventSequenceNumber.Unavailable;
    EventSourceId? _eventSourceId;
    EventStreamType? _eventStreamType;
    EventStreamId? _eventStreamId;
    EventSourceType? _eventSourceType;

    /// <summary>
    /// Sets the <see cref="EventSequenceNumber"/> for the concurrency scope.
    /// </summary>
    /// <param name="sequenceNumber"><see cref="EventSequenceNumber"/> to scope to.</param>
    /// <returns><see cref="ConcurrencyScopeBuilder"/> for continuation.</returns>
    public ConcurrencyScopeBuilder WithSequenceNumber(EventSequenceNumber sequenceNumber)
    {
        _sequenceNumber = sequenceNumber;
        return this;
    }

    /// <summary>
    /// Sets the <see cref="EventSourceId"/> for the concurrency scope.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to scope to.</param>
    /// <returns><see cref="ConcurrencyScopeBuilder"/> for continuation.</returns>
    public ConcurrencyScopeBuilder WithEventSourceId(EventSourceId eventSourceId)
    {
        _eventSourceId = eventSourceId;
        return this;
    }

    /// <summary>
    /// Sets the <see cref="EventStreamType"/> for the concurrency scope.
    /// </summary>
    /// <param name="eventStreamType"><see cref="EventStreamType"/> to scope to.</param>
    /// <returns><see cref="ConcurrencyScopeBuilder"/> for continuation.</returns>
    public ConcurrencyScopeBuilder WithEventStreamType(EventStreamType eventStreamType)
    {
        _eventStreamType = eventStreamType;
        return this;
    }

    /// <summary>
    /// Sets the <see cref="EventStreamId"/> for the concurrency scope.
    /// </summary>
    /// <param name="eventStreamId"><see cref="EventStreamId"/> to scope to.</param>
    /// <returns><see cref="ConcurrencyScopeBuilder"/> for continuation.</returns>
    public ConcurrencyScopeBuilder WithEventStreamId(EventStreamId eventStreamId)
    {
        _eventStreamId = eventStreamId;
        return this;
    }

    /// <summary>
    /// Sets the <see cref="EventSourceType"/> for the concurrency scope.
    /// </summary>
    /// <param name="eventSourceType"><see cref="EventSourceType"/> to scope to.</param>
    /// <returns><see cref="ConcurrencyScopeBuilder"/> for continuation.</returns>
    public ConcurrencyScopeBuilder WithEventSourceType(EventSourceType eventSourceType)
    {
        _eventSourceType = eventSourceType;
        return this;
    }

    /// <summary>
    /// Adds a <see cref="EventType"/> for the concurrency scope.
    /// </summary>
    /// <typeparam name="T">Type of the event to scope to.</typeparam>
    /// <returns><see cref="ConcurrencyScopeBuilder"/> for continuation.</returns>
    public ConcurrencyScopeBuilder WithEventType<T>() => WithEventType(typeof(T));

    /// <summary>
    /// Adds a <see cref="EventType"/> for the concurrency scope.
    /// </summary>
    /// <param name="type">Type of the event to scope to.</param>
    /// <returns><see cref="ConcurrencyScopeBuilder"/> for continuation.</returns>
    public ConcurrencyScopeBuilder WithEventType(Type type) => WithEventType(type.GetEventType());

    /// <summary>
    /// Adds a <see cref="EventType"/> for the concurrency scope.
    /// </summary>
    /// <param name="eventType"><see cref="EventType"/> to scope to.</param>
    /// <returns><see cref="ConcurrencyScopeBuilder"/> for continuation.</returns>
    public ConcurrencyScopeBuilder WithEventType(EventType eventType)
    {
        _eventTypes.Add(eventType);
        return this;
    }

    /// <summary>
    /// Adds a collection of <see cref="EventType"/>s for the concurrency scope.
    /// </summary>
    /// <param name="eventTypes">Collection of <see cref="EventType"/>s to scope to.</param>
    /// <returns><see cref="ConcurrencyScopeBuilder"/> for continuation.</returns>
    public ConcurrencyScopeBuilder WithEventTypes(IEnumerable<EventType> eventTypes)
    {
        _eventTypes.AddRange(eventTypes);
        return this;
    }

    /// <summary>
    /// Builds the <see cref="ConcurrencyScope"/> with the configured properties.
    /// </summary>
    /// <returns>A new instance of <see cref="ConcurrencyScope"/>.</returns>
    public ConcurrencyScope Build() => new(
            _sequenceNumber,
            _eventSourceId,
            _eventStreamType,
            _eventStreamId,
            _eventSourceType,
            _eventTypes.Distinct().ToArray());
}
