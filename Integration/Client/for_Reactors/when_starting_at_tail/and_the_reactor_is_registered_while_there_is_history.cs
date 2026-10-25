// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_Reactors.when_starting_at_tail.and_the_reactor_is_registered_while_there_is_history.context;

namespace Cratis.Chronicle.Integration.for_Reactors.when_starting_at_tail;

/// <summary>
/// The control for this is and_reactor_is_registered_while_there_are_events_in_sequence, where the same
/// registration without the attribute catches up on every historical event.
/// </summary>
/// <param name="context">The <see cref="context"/> the specs run against.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_reactor_is_registered_while_there_is_history(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_disconnected_reactor_starting_at_tail(chronicleFixture)
    {
        public int HistoricalEvents;
        public EventSequenceNumber LastEventSequenceNumber;

        async Task Establish()
        {
            var history = EventForEventSourceIdHelpers.CreateMultiple(_ => new SomeEvent(42), 10, "Partition").ToList();
            HistoricalEvents = history.Count;
            await EventStore.EventLog.AppendMany(history);
        }

        async Task Because()
        {
            var reactor = await EventStore.Reactors.Register<ReactorStartingAtTail>();
            await reactor.WaitTillSubscribed();

            var result = await EventStore.EventLog.Append("Partition", new SomeEvent(43));
            LastEventSequenceNumber = result.SequenceNumber;
            await Reactor.WaitTillHandledEventReaches(1);
            await reactor.WaitTillReachesEventSequenceNumber(LastEventSequenceNumber);
        }
    }

    [Fact] void should_handle_only_what_was_appended_after_it_registered() => Context.Reactor.HandledEvents.ShouldEqual(1);
    [Fact] void should_not_have_history_to_handle() => Context.HistoricalEvents.ShouldBeGreaterThan(Context.Reactor.HandledEvents);
}
