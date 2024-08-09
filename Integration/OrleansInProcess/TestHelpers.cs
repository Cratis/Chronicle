// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.OrleansInProcess;

public static class TestHelpers
{
    public static async Task ShouldHaveStoredCorrectEvent<TEvent>(this IntegrationTestSetup fixture, Concepts.Events.EventSequenceNumber sequenceNumber, Concepts.Events.EventSourceId eventSourceId, Action<TEvent> isCorrectEvent)
    {
        var eventLog = fixture.GetEventLogStorage();
        var evt = await eventLog.GetEventAt(sequenceNumber);
        var eventClrType = typeof(TEvent);
        var eventType = fixture.EventStore.EventTypes.GetEventTypeFor(eventClrType);
        evt.Context.EventSourceId.Value.ShouldEqual(eventSourceId.Value);
        evt.Metadata.SequenceNumber.ShouldEqual(sequenceNumber);
        evt.Metadata.Type.Id.Value.ShouldEqual(eventType.Id.Value);
        var eventObject = await fixture.Services.GetRequiredService<IEventSerializer>().Deserialize(eventClrType, evt.Content);
        eventObject.ShouldBeOfExactType<TEvent>();
        var theEvent = (TEvent)eventObject;
        isCorrectEvent(theEvent);
    }

    public static async Task ShouldHaveCorrectNextSequenceNumber(this IntegrationTestSetup fixture, Concepts.Events.EventSequenceNumber sequenceNumber)
    {
        var eventLog = fixture.EventLogSequenceGrain;
        var number = await eventLog.GetNextSequenceNumber();
        number.ShouldEqual(sequenceNumber);
    }

    public static async Task ShouldHaveCorrectTailSequenceNumber(this IntegrationTestSetup fixture, Concepts.Events.EventSequenceNumber sequenceNumber)
    {
        var eventLog = fixture.EventLogSequenceGrain;
        var number = await eventLog.GetTailSequenceNumber();
        number.ShouldEqual(sequenceNumber);

        var storedEventLog = fixture.GetEventLogStorage();
        var storedNumber = await storedEventLog.GetTailSequenceNumber();
        storedNumber.ShouldEqual(sequenceNumber);
    }
}