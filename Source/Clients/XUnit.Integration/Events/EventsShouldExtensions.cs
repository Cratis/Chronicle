// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
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
        var events = await fixture.EventStore.EventLog.GetFromSequenceNumber(sequenceNumber);
        var @event = events.FirstOrDefault(_ => _.Context.SequenceNumber == sequenceNumber);
        Assert.NotNull(@event);
        Assert.Equal(@event.Context.EventSourceId.Value, eventSourceId.Value);
        Assert.Equal(@event.Context.SequenceNumber.Value, sequenceNumber.Value);
        Assert.IsType<TEvent>(@event.Content);
        validator((TEvent)@event.Content);
    }

    /// <summary>
    /// Asserts that the event log has a next sequence number.
    /// </summary>
    /// <param name="fixture">The fixture to use.</param>
    /// <param name="sequenceNumber">The expected next sequence number.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task ShouldHaveNextSequenceNumber(this IChronicleSetupFixture fixture, EventSequenceNumber sequenceNumber)
    {
        var number = await fixture.EventStore.EventLog.GetNextSequenceNumber();
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
        var number = await fixture.EventStore.EventLog.GetTailSequenceNumber();
        Assert.Equal(number.Value, sequenceNumber.Value);
    }
}
