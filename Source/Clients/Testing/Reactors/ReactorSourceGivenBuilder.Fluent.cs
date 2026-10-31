// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// Represents event input from one source to a fluent reactor scenario.
/// </summary>
/// <param name="scenario">The scenario to drive.</param>
/// <param name="eventSourceId">The source of the events.</param>
public class ReactorSourceGivenBuilder(ReactorScenario scenario, EventSourceId eventSourceId)
{
    readonly EventSourceType _eventSourceType = EventSourceType.Default;
    readonly EventStreamType _eventStreamType = EventStreamType.All;
    readonly EventStreamId _eventStreamId = EventStreamId.Default;

    ReactorSourceGivenBuilder(
        ReactorScenario scenario,
        EventSourceId eventSourceId,
        EventSourceType eventSourceType,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId)
        : this(scenario, eventSourceId)
    {
        _eventSourceType = eventSourceType;
        _eventStreamType = eventStreamType;
        _eventStreamId = eventStreamId;
    }

    /// <summary>
    /// Creates a builder that seeds events on the specified event source and stream route.
    /// </summary>
    /// <param name="eventSourceType">The event source type, or <see langword="null"/> for <see cref="EventSourceType.Default"/>.</param>
    /// <param name="eventStreamType">The event stream type, or <see langword="null"/> for <see cref="EventStreamType.All"/>.</param>
    /// <param name="eventStreamId">The event stream identifier, or <see langword="null"/> for <see cref="EventStreamId.Default"/>.</param>
    /// <returns>A new builder carrying the route, leaving this builder unchanged.</returns>
    /// <remarks>
    /// The route is included in each seeded event's context. Omitted values use the same defaults as
    /// appending to the event log.
    /// </remarks>
    public ReactorSourceGivenBuilder OnStream(
        EventSourceType? eventSourceType = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default) =>
        new(scenario, eventSourceId, eventSourceType ?? EventSourceType.Default, eventStreamType ?? EventStreamType.All, eventStreamId ?? EventStreamId.Default);

    /// <summary>
    /// Provides events in order and awaits every applicable callback.
    /// </summary>
    /// <param name="events">The events to provide.</param>
    /// <returns>A task completing after all callbacks complete.</returns>
    public Task Events(params object[] events) => scenario.InvokeWith(eventSourceId, events, _eventSourceType, _eventStreamType, _eventStreamId);

    /// <summary>
    /// Records one event with its scenario-local context without delivering it or appending it to an event store.
    /// </summary>
    /// <param name="event">The event to record.</param>
    /// <returns>The recorded occurrence for later redelivery.</returns>
    public async Task<RecordedOccurrence> History(object @event) =>
        (await History([@event]))[0];

    /// <summary>
    /// Records events in order without delivering them or appending them to an event store.
    /// </summary>
    /// <param name="events">The events to record.</param>
    /// <returns>The occurrences with their original contexts for later redelivery.</returns>
    public Task<IReadOnlyList<RecordedOccurrence>> History(params object[] events) =>
        scenario.RecordHistory(eventSourceId, events, _eventSourceType, _eventStreamType, _eventStreamId);
}
