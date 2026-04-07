// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.InProcess.Integration.for_EventAppendCollection.when_a_reactor_handles_two_events.and_collecting_both_appended_events.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventAppendCollection.when_a_reactor_handles_two_events;

[Collection(ChronicleCollection.Name)]
public class and_collecting_both_appended_events(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : given.an_event_append_collection_scope(fixture)
    {
        async Task Because()
        {
            var reactor = EventStore.Reactors.GetHandlerFor<AReactor>();
            await reactor.WaitTillActive();

            AppendedEventsCollector = StartCollectingAppends();
            await EventStore.EventLog.Append(EventSourceId, new AnEventHappened(1));
            await EventStore.EventLog.Append(EventSourceId, new AnotherEventHappened(2));
        }
    }

    [Fact] void should_collect_two_events() => Context.AppendedEventsCollector.All.Count.ShouldEqual(2);
    [Fact] void should_have_the_first_event() => Context.AppendedEventsCollector.All[0].Event.Content.ShouldBeOfExactType<AnEventHappened>();
    [Fact] void should_have_the_second_event() => Context.AppendedEventsCollector.All[1].Event.Content.ShouldBeOfExactType<AnotherEventHappened>();
    [Fact] void should_both_be_successful() => Context.AppendedEventsCollector.All.All(e => e.Result.IsSuccess).ShouldBeTrue();
    [Fact] void should_both_be_for_the_correct_event_source() => Context.AppendedEventsCollector.All.All(e => e.Event.Context.EventSourceId == Context.EventSourceId).ShouldBeTrue();
}
