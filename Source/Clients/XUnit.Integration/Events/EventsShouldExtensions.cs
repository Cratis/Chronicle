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
    public static void ShouldHaveAppendedEvent<TEvent>(this IntegrationSpecificationContext fixture, EventSequenceNumber sequenceNumber, EventSourceId eventSourceId, Action<TEvent> validator)
    {
        var eventLog = fixture.GetEventLogStorage();
        var @event = eventLog.GetEventAt(sequenceNumber.Value).GetAwaiter().GetResult();
        var eventClrType = typeof(TEvent);
        var eventType = fixture.EventStore.EventTypes.GetEventTypeFor(eventClrType);
        Assert.Equal(@event.Context.EventSourceId.Value, eventSourceId.Value);
        Assert.Equal(@event.Metadata.SequenceNumber.Value, sequenceNumber.Value);
        Assert.Equal(@event.Metadata.Type.Id.Value, eventType.Id.Value);
        var eventObject = fixture.Services.GetRequiredService<IEventSerializer>().Deserialize(eventClrType, @event.Content).GetAwaiter().GetResult();
        Assert.IsType<TEvent>(eventObject);
        var theEvent = (TEvent)eventObject;
        validator(theEvent);
    }

    /// <summary>
    /// Asserts that the event log has a next sequence number.
    /// </summary>
    /// <param name="fixture">The fixture to use.</param>
    /// <param name="sequenceNumber">The expected next sequence number.</param>
    public static void ShouldHaveNextSequenceNumber(this IntegrationSpecificationContext fixture, EventSequenceNumber sequenceNumber)
    {
        var eventLog = fixture.EventLogSequenceGrain;
        var number = eventLog.GetNextSequenceNumber().GetAwaiter().GetResult();
        Assert.Equal(number.Value, sequenceNumber.Value);
    }

    /// <summary>
    /// Asserts that the event log has a tail sequence number.
    /// </summary>
    /// <param name="fixture">The fixture to use.</param>
    /// <param name="sequenceNumber">The expected tail sequence number.</param>
    public static void ShouldHaveTailSequenceNumber(this IntegrationSpecificationContext fixture, EventSequenceNumber sequenceNumber)
    {
        var eventLog = fixture.EventLogSequenceGrain;
        var number = eventLog.GetTailSequenceNumber().GetAwaiter().GetResult();
        Assert.Equal(number.Value, sequenceNumber.Value);

        var storedEventLog = fixture.GetEventLogStorage();
        var storedNumber = storedEventLog.GetTailSequenceNumber().GetAwaiter().GetResult();
        Assert.Equal(storedNumber.Value, sequenceNumber.Value);
    }
}
