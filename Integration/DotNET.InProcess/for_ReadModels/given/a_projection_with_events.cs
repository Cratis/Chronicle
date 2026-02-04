// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.InProcess.Integration.for_ReadModels.given;

public class a_projection_with_events(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
{
    public EventSourceId EventSourceId;
    public SomeEvent FirstEvent;
    public AnotherEvent SecondEvent;

    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent), typeof(AnotherEvent)];
    public override IEnumerable<Type> Projections => [typeof(SomeProjection)];

    protected void Establish()
    {
        EventSourceId = "some-source";
        FirstEvent = new SomeEvent(42);
        SecondEvent = new AnotherEvent("test-value");
    }

    protected async Task AppendEvents()
    {
        await EventStore.EventLog.Append(EventSourceId, FirstEvent);
        await EventStore.EventLog.Append(EventSourceId, SecondEvent);
    }
}
