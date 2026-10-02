// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Extension methods for appending to an <see cref="IEventSequence"/> through a registered event source.
/// </summary>
/// <remarks>
/// These are extension methods rather than overloads on <see cref="IEventSequence"/> so that existing callers and
/// test doubles of the interface keep resolving to the same members.
/// </remarks>
public static class EventSequenceEventSourceExtensions
{
    /// <summary>
    /// Appends an event through a registered event source definition.
    /// </summary>
    /// <typeparam name="TSource">The <see cref="IEventSource"/> to append through.</typeparam>
    /// <param name="eventSequence">The <see cref="IEventSequence"/> to append to.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> of the event source instance.</param>
    /// <param name="event">The event to append.</param>
    /// <param name="eventStream">Optional name of a stream declared by the event source.</param>
    /// <param name="eventStreamId">Optional <see cref="EventStreamId"/> within the stream. The format is left to the caller.</param>
    /// <param name="correlationId">Optional <see cref="CorrelationId"/> of the event.</param>
    /// <param name="tags">Optional tags to associate with the event.</param>
    /// <param name="concurrencyScope">Optional <see cref="ConcurrencyScope"/>. When not set it is narrowed to the dimensions the definition declares.</param>
    /// <param name="occurred">Optional time the event occurred.</param>
    /// <param name="subject">Optional <see cref="Subject"/> of the event.</param>
    /// <returns><see cref="AppendResult"/> with details about whether or not it succeeded and more.</returns>
    /// <exception cref="UnknownEventSource">The type is not a discovered event source.</exception>
    /// <exception cref="EventStreamDoesNotBelongToEventSource">The stream is not declared by the event source.</exception>
    public static Task<AppendResult> Append<TSource>(
        this IEventSequence eventSequence,
        EventSourceId eventSourceId,
        object @event,
        string? eventStream = default,
        EventStreamId? eventStreamId = default,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        ConcurrencyScope? concurrencyScope = default,
        DateTimeOffset? occurred = default,
        Subject? subject = default)
        where TSource : IEventSource =>
        eventSequence.AppendThroughEventSource(typeof(TSource), eventSourceId, @event, eventStream, eventStreamId, correlationId, tags, concurrencyScope, occurred, subject);

    /// <summary>
    /// Appends an event through a registered event source definition.
    /// </summary>
    /// <param name="eventSequence">The <see cref="IEventSequence"/> to append to.</param>
    /// <param name="eventSource">The type of the <see cref="IEventSource"/> to append through.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> of the event source instance.</param>
    /// <param name="event">The event to append.</param>
    /// <param name="eventStream">Optional name of a stream declared by the event source.</param>
    /// <param name="eventStreamId">Optional <see cref="EventStreamId"/> within the stream. The format is left to the caller.</param>
    /// <param name="correlationId">Optional <see cref="CorrelationId"/> of the event.</param>
    /// <param name="tags">Optional tags to associate with the event.</param>
    /// <param name="concurrencyScope">Optional <see cref="ConcurrencyScope"/>. When not set it is narrowed to the dimensions the definition declares.</param>
    /// <param name="occurred">Optional time the event occurred.</param>
    /// <param name="subject">Optional <see cref="Subject"/> of the event.</param>
    /// <returns><see cref="AppendResult"/> with details about whether or not it succeeded and more.</returns>
    /// <exception cref="UnknownEventSource">The type is not a discovered event source.</exception>
    /// <exception cref="EventStreamDoesNotBelongToEventSource">The stream is not declared by the event source.</exception>
    public static Task<AppendResult> Append(
        this IEventSequence eventSequence,
        Type eventSource,
        EventSourceId eventSourceId,
        object @event,
        string? eventStream = default,
        EventStreamId? eventStreamId = default,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        ConcurrencyScope? concurrencyScope = default,
        DateTimeOffset? occurred = default,
        Subject? subject = default) =>
        eventSequence.AppendThroughEventSource(eventSource, eventSourceId, @event, eventStream, eventStreamId, correlationId, tags, concurrencyScope, occurred, subject);

    /// <summary>
    /// Appends several events through a registered event source definition.
    /// </summary>
    /// <typeparam name="TSource">The <see cref="IEventSource"/> to append through.</typeparam>
    /// <param name="eventSequence">The <see cref="IEventSequence"/> to append to.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> of the event source instance.</param>
    /// <param name="events">The events to append.</param>
    /// <param name="eventStream">Optional name of a stream declared by the event source.</param>
    /// <param name="eventStreamId">Optional <see cref="EventStreamId"/> within the stream. The format is left to the caller.</param>
    /// <param name="correlationId">Optional <see cref="CorrelationId"/> of the events.</param>
    /// <param name="tags">Optional tags to associate with the events.</param>
    /// <param name="concurrencyScope">Optional <see cref="ConcurrencyScope"/>. When not set it is narrowed to the dimensions the definition declares.</param>
    /// <param name="occurred">Optional time the events occurred.</param>
    /// <param name="subject">Optional <see cref="Subject"/> of the events.</param>
    /// <returns><see cref="AppendManyResult"/> with details about whether or not it succeeded and more.</returns>
    /// <exception cref="UnknownEventSource">The type is not a discovered event source.</exception>
    /// <exception cref="EventStreamDoesNotBelongToEventSource">The stream is not declared by the event source.</exception>
    public static Task<AppendManyResult> AppendMany<TSource>(
        this IEventSequence eventSequence,
        EventSourceId eventSourceId,
        IEnumerable<object> events,
        string? eventStream = default,
        EventStreamId? eventStreamId = default,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        ConcurrencyScope? concurrencyScope = default,
        DateTimeOffset? occurred = default,
        Subject? subject = default)
        where TSource : IEventSource =>
        eventSequence.AppendManyThroughEventSource(typeof(TSource), eventSourceId, events, eventStream, eventStreamId, correlationId, tags, concurrencyScope, occurred, subject);

    /// <summary>
    /// Appends several events through a registered event source definition.
    /// </summary>
    /// <param name="eventSequence">The <see cref="IEventSequence"/> to append to.</param>
    /// <param name="eventSource">The type of the <see cref="IEventSource"/> to append through.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> of the event source instance.</param>
    /// <param name="events">The events to append.</param>
    /// <param name="eventStream">Optional name of a stream declared by the event source.</param>
    /// <param name="eventStreamId">Optional <see cref="EventStreamId"/> within the stream. The format is left to the caller.</param>
    /// <param name="correlationId">Optional <see cref="CorrelationId"/> of the events.</param>
    /// <param name="tags">Optional tags to associate with the events.</param>
    /// <param name="concurrencyScope">Optional <see cref="ConcurrencyScope"/>. When not set it is narrowed to the dimensions the definition declares.</param>
    /// <param name="occurred">Optional time the events occurred.</param>
    /// <param name="subject">Optional <see cref="Subject"/> of the events.</param>
    /// <returns><see cref="AppendManyResult"/> with details about whether or not it succeeded and more.</returns>
    /// <exception cref="UnknownEventSource">The type is not a discovered event source.</exception>
    /// <exception cref="EventStreamDoesNotBelongToEventSource">The stream is not declared by the event source.</exception>
    public static Task<AppendManyResult> AppendMany(
        this IEventSequence eventSequence,
        Type eventSource,
        EventSourceId eventSourceId,
        IEnumerable<object> events,
        string? eventStream = default,
        EventStreamId? eventStreamId = default,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        ConcurrencyScope? concurrencyScope = default,
        DateTimeOffset? occurred = default,
        Subject? subject = default) =>
        eventSequence.AppendManyThroughEventSource(eventSource, eventSourceId, events, eventStream, eventStreamId, correlationId, tags, concurrencyScope, occurred, subject);
}
