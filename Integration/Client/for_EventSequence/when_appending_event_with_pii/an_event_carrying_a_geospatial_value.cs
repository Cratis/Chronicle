// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Geospatial;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending_event_with_pii.an_event_carrying_a_geospatial_value.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_event_with_pii;

/// <summary>
/// The <c>[PII]</c> property turns compliance handling on for the whole event, so the geospatial value beside it
/// is walked too. It is a schema leaf carrying only its format while the value on the wire is a GeoJSON object,
/// which the walk used to read as properties the schema had lost - failing the append outright.
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class an_event_carrying_a_geospatial_value(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId { get; } = "some-sighting";
        public SomeEventWithPIIAndGeospatialValue Event { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEventWithPIIAndGeospatialValue)];

        void Establish() => Event = new SomeEventWithPIIAndGeospatialValue("John Doe", new Point(10.7522, 59.9139));

        async Task Because() => await EventStore.EventLog.Append(EventSourceId, Event);
    }

    [Fact] Task should_have_appended_the_event() => Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);

    [Fact]
    Task should_read_the_event_back_whole() =>
        Context.ShouldHaveAppendedEvent<SomeEventWithPIIAndGeospatialValue>(
            EventSequenceNumber.First.Value,
            Context.EventSourceId.Value,
            readEvent =>
            {
                readEvent.ReportedBy.ShouldEqual(Context.Event.ReportedBy);
                readEvent.ObservedAt.ShouldEqual(Context.Event.ObservedAt);
            });
}
