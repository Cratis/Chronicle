// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Execution;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a lightweight, in-process scenario for testing <see cref="IEventSequence"/> operations without any infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="Given(EventForEventSourceId[])"/> to seed pre-existing events into the in-memory event log before
/// exercising production code via <see cref="EventSequence"/> or <see cref="EventLog"/>.
/// </para>
/// <para>
/// Create a new <see cref="EventScenario"/> instance per test to keep tests isolated; the in-memory
/// event log accumulates state across calls on the same instance.
/// </para>
/// <para>
/// Usage:
/// <code>
/// var scenario = new EventScenario();
/// await scenario.Given(new EventForEventSourceId(myId, new SomeEvent("value"), Causation.Unknown()));
/// var result = await scenario.EventLog.Append(myId, new OtherEvent("other"));
/// result.ShouldBeSuccessful();
/// </code>
/// </para>
/// </remarks>
/// <param name="defaults">The <see cref="Defaults"/> to use for service resolution.</param>
public class EventScenario(Defaults defaults)
{
    readonly InMemoryEventLog _eventLog = new(defaults.EventTypes);
    readonly IEventTypes _eventTypes = defaults.EventTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventScenario"/> class using <see cref="Defaults.Instance"/>.
    /// </summary>
    public EventScenario()
        : this(Defaults.Instance)
    {
    }

    /// <summary>
    /// Gets the in-memory <see cref="IEventLog"/> that can be used to perform <c>Append</c> and <c>AppendMany</c> operations.
    /// </summary>
    public IEventLog EventLog => _eventLog;

    /// <summary>
    /// Gets the in-memory <see cref="IEventSequence"/> that can be used to perform <c>Append</c> and <c>AppendMany</c> operations.
    /// </summary>
    /// <remarks>
    /// This is the same underlying instance as <see cref="EventLog"/>.
    /// </remarks>
    public IEventSequence EventSequence => _eventLog;

    /// <summary>
    /// Seeds the event sequence with pre-existing events, each associated with their own <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="events">The events to seed. Each entry carries its own <see cref="EventSourceId"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task Given(params EventForEventSourceId[] events)
    {
        var baseSequenceNumber = (await _eventLog.GetNextSequenceNumber()).Value;

        var seededEvents = events.Select((@event, index) =>
        {
            var eventType = _eventTypes.GetEventTypeFor(@event.Event.GetType());
            return new AppendedEvent(
                EventContext.From(
                    EventStoreName.NotSet,
                    EventStoreNamespaceName.NotSet,
                    eventType,
                    @event.EventSourceType,
                    @event.EventSourceId,
                    @event.EventStreamType,
                    @event.EventStreamId,
                    (EventSequenceNumber)(baseSequenceNumber + (ulong)index),
                    CorrelationId.New(),
                    @event.Occurred),
                @event.Event.AsExpandoObject(true));
        });

        _eventLog.Seed(seededEvents);
    }

    /// <summary>
    /// Seeds the event sequence with pre-existing events, all sharing the same <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to associate all events with.</param>
    /// <param name="events">The event instances to seed, in order.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task Given(EventSourceId eventSourceId, params object[] events) =>
        Given(events.Select(e => new EventForEventSourceId(eventSourceId, e, Causation.Unknown())).ToArray());
}
