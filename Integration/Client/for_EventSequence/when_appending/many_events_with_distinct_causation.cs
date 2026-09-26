// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending.many_events_with_distinct_causation.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class many_events_with_distinct_causation(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public IReadOnlyList<AppendedEvent> AppendedEvents;
        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        async Task Because()
        {
            await EventStore.EventLog.AppendMany([
                new EventForEventSourceId("first", new SomeEvent("first"), new Causation(DateTimeOffset.UnixEpoch, "first-cause", new Dictionary<string, string>())),
                new EventForEventSourceId("second", new SomeEvent("second"), new Causation(DateTimeOffset.UnixEpoch, "second-cause", new Dictionary<string, string>()))
            ]);
            AppendedEvents = await EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
        }
    }

    [Fact] void should_store_the_first_events_cause() => Context.AppendedEvents[0].Context.Causation.Last().Type.Name.ShouldEqual("first-cause");
    [Fact] void should_store_the_second_events_cause() => Context.AppendedEvents[1].Context.Causation.Last().Type.Name.ShouldEqual("second-cause");
}
