// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Defines a transactional event sequence.
/// </summary>
public interface ITransactionalEventSequence
{
    /// <summary>
    /// Gets the current <see cref="IUnitOfWork"/>.
    /// </summary>
    IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// Append a single event to the event store.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
    /// <param name="event">The event.</param>
    /// <param name="eventStreamType">Optional <see cref="EventStreamType"/> to append to. Defaults to <see cref="EventStreamType.All"/>.</param>
    /// <param name="eventStreamId">Optional <see cref="EventStreamId"/> to append to. Defaults to <see cref="EventStreamId.Default"/>.</param>
    /// <param name="eventSourceType">Optional <see cref="EventSourceType"/> to append to. Defaults to <see cref="EventSourceType.Default"/>.</param>
    /// <param name="concurrencyScope">Optional <see cref="ConcurrencyScope"/> to use for concurrency control. Defaults to <see cref="ConcurrencyScope.None"/>.</param>
    /// <param name="tags">Optional dynamic tags to associate with the event.</param>
    /// <param name="occurred">Optional timestamp for when the event occurred.</param>
    /// <param name="subject">Optional subject identifying what the event is about.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task Append(
        EventSourceId eventSourceId,
        object @event,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        ConcurrencyScope? concurrencyScope = default,
        IEnumerable<string>? tags = default,
        DateTimeOffset? occurred = default,
        Subject? subject = default);

    /// <summary>
    /// Append an event with structured named tags to the current unit of work.
    /// </summary>
    /// <param name="eventSourceId">The event source.</param>
    /// <param name="event">The event.</param>
    /// <param name="namedTags">Structured named tags.</param>
    /// <param name="eventStreamType">Optional stream type.</param>
    /// <param name="eventStreamId">Optional stream id.</param>
    /// <param name="eventSourceType">Optional source type.</param>
    /// <param name="concurrencyScope">Optional concurrency scope.</param>
    /// <param name="tags">Optional plain tags.</param>
    /// <param name="occurred">Optional occurred time.</param>
    /// <param name="subject">Optional subject.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="NamedTagsNotSupported">The implementation cannot carry nonempty named tags.</exception>
    Task AppendWithNamedTags(
        EventSourceId eventSourceId,
        object @event,
        IEnumerable<NamedTag> namedTags,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        ConcurrencyScope? concurrencyScope = default,
        IEnumerable<string>? tags = default,
        DateTimeOffset? occurred = default,
        Subject? subject = default) =>
        namedTags.Any()
            ? throw new NamedTagsNotSupported(GetType())
            : Append(eventSourceId, @event, eventStreamType, eventStreamId, eventSourceType, concurrencyScope, tags, occurred, subject);

    /// <summary>
    /// Append a collection of events to the event store.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
    /// <param name="events">Collection of events to append.</param>
    /// <param name="eventStreamType">Optional <see cref="EventStreamType"/> to append to. Defaults to <see cref="EventStreamType.All"/>.</param>
    /// <param name="eventStreamId">Optional <see cref="EventStreamId"/> to append to. Defaults to <see cref="EventStreamId.Default"/>.</param>
    /// <param name="eventSourceType">Optional <see cref="EventSourceType"/> to append to. Defaults to <see cref="EventSourceType.Default"/>.</param>
    /// <param name="concurrencyScope">Optional <see cref="ConcurrencyScope"/> to use for concurrency control. Defaults to <see cref="ConcurrencyScope.None"/>.</param>
    /// <param name="tags">Optional dynamic tags to associate with the events.</param>
    /// <param name="occurred">Optional timestamp for when the events occurred.</param>
    /// <param name="subject">Optional subject identifying what the events are about.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task AppendMany(
        EventSourceId eventSourceId,
        IEnumerable<object> events,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        ConcurrencyScope? concurrencyScope = default,
        IEnumerable<string>? tags = default,
        DateTimeOffset? occurred = default,
        Subject? subject = default);

    /// <summary>
    /// Append events with structured named tags to the current unit of work.
    /// </summary>
    /// <param name="eventSourceId">The event source.</param>
    /// <param name="events">The events.</param>
    /// <param name="namedTags">Structured named tags for every event.</param>
    /// <param name="eventStreamType">Optional stream type.</param>
    /// <param name="eventStreamId">Optional stream id.</param>
    /// <param name="eventSourceType">Optional source type.</param>
    /// <param name="concurrencyScope">Optional concurrency scope.</param>
    /// <param name="tags">Optional plain tags.</param>
    /// <param name="occurred">Optional occurred time.</param>
    /// <param name="subject">Optional subject.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="NamedTagsNotSupported">The implementation cannot carry nonempty named tags.</exception>
    Task AppendManyWithNamedTags(
        EventSourceId eventSourceId,
        IEnumerable<object> events,
        IEnumerable<NamedTag> namedTags,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        ConcurrencyScope? concurrencyScope = default,
        IEnumerable<string>? tags = default,
        DateTimeOffset? occurred = default,
        Subject? subject = default) =>
        namedTags.Any()
            ? throw new NamedTagsNotSupported(GetType())
            : AppendMany(eventSourceId, events, eventStreamType, eventStreamId, eventSourceType, concurrencyScope, tags, occurred, subject);
}
