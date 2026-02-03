// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cratis.Chronicle.XUnit.Integration.Events;

/// <summary>
/// Should extensions for asserting state related to events and event sequences.
/// </summary>
public static class EventsShouldExtensions
{
    /// <summary>
    /// Asserts that an event has been appended to the event log.
    /// </summary>
    /// <typeparam name="TEvent">Type of the event.</typeparam>
    /// <param name="fixture">The fixture to use.</param>
    /// <param name="sequenceNumber">The sequence number of the event.</param>
    /// <param name="eventSourceId">The event source ID.</param>
    /// <param name="validator">Action to validate the event.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task ShouldHaveAppendedEvent<TEvent>(this IChronicleSetupFixture fixture, EventSequenceNumber sequenceNumber, EventSourceId eventSourceId, Action<TEvent> validator)
    {
        var eventLog = fixture.GetEventLogStorage();
        var @event = await eventLog.GetEventAt(sequenceNumber.Value);
        var eventClrType = typeof(TEvent);
        var eventType = fixture.EventStore.EventTypes.GetEventTypeFor(eventClrType);
        Assert.Equal(@event.Context.EventSourceId.Value, eventSourceId.Value);
        Assert.Equal(@event.Context.SequenceNumber.Value, sequenceNumber.Value);
        Assert.Equal(@event.Context.EventType.Id.Value, eventType.Id.Value);
        var contentAsJson = System.Text.Json.JsonSerializer.SerializeToNode(@event.Content)!.AsObject();
        var eventObject = await fixture.Services.GetRequiredService<IEventSerializer>().Deserialize(eventClrType, contentAsJson);
        Assert.IsType<TEvent>(eventObject);
        var theEvent = (TEvent)eventObject;
        validator(theEvent);
    }

    /// <summary>
    /// Asserts that the event log has a next sequence number.
    /// </summary>
    /// <param name="fixture">The fixture to use.</param>
    /// <param name="sequenceNumber">The expected next sequence number.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task ShouldHaveNextSequenceNumber(this IChronicleSetupFixture fixture, EventSequenceNumber sequenceNumber)
    {
        var eventLog = fixture.EventLogSequenceGrain;
        var number = await eventLog.GetNextSequenceNumber();
        Assert.Equal(number.Value, sequenceNumber.Value);
    }

    /// <summary>
    /// Asserts that the event log has a tail sequence number.
    /// </summary>
    /// <param name="fixture">The fixture to use.</param>
    /// <param name="sequenceNumber">The expected tail sequence number.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task ShouldHaveTailSequenceNumber(this IChronicleSetupFixture fixture, EventSequenceNumber sequenceNumber)
    {
        var eventLog = fixture.EventLogSequenceGrain;
        var number = await eventLog.GetTailSequenceNumber();
        Assert.Equal(number.Value, sequenceNumber.Value);

        var storedEventLog = fixture.GetEventLogStorage();
        var storedNumber = await storedEventLog.GetTailSequenceNumber();
        Assert.Equal(storedNumber.Value, sequenceNumber.Value);
    }
}
