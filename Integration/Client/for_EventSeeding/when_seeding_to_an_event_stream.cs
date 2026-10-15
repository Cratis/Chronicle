// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

using context = Cratis.Chronicle.Integration.for_EventSeeding.when_seeding_to_an_event_stream.context;

namespace Cratis.Chronicle.Integration.for_EventSeeding;

/// <summary>
/// Verifies seeding preserves stream routing through the transport and kernel.
/// </summary>
/// <param name="context">The integration context.</param>
[Collection(ChronicleCollection.Name)]
public class when_seeding_to_an_event_stream(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public IEnumerable<AppendedEvent> Events;
        string _source;

        public override IEnumerable<Type> EventTypes => [typeof(OfficeOpened)];

        void Establish() => _source = Guid.NewGuid().ToString();

        async Task Because()
        {
            EventStore.Seeding.For(_source, "Offices", "office-1", [new OfficeOpened("Oslo")], "Company");
            EventStore.Seeding.For(_source, "Offices", "office-2", [new OfficeOpened("Bergen")], "Company");
            await EventStore.Seeding.Register();
            Events = await EventStore.EventLog.GetForEventSourceIdAndEventTypes(_source, [EventStore.EventTypes.GetEventTypeFor(typeof(OfficeOpened))], "Offices", "office-1", "Company");
        }
    }

    [Fact] void should_read_only_the_requested_stream() => Context.Events.Count().ShouldEqual(1);
    [Fact] void should_preserve_stream_id() => Context.Events.Single().Context.EventStreamId.Value.ShouldEqual("office-1");
    [Fact] void should_preserve_source_type() => Context.Events.Single().Context.EventSourceType.Value.ShouldEqual("Company");
}
