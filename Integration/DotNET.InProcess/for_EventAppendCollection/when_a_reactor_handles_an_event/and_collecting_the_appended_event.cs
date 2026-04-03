// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.InProcess.Integration.for_EventAppendCollection.when_a_reactor_handles_an_event.and_collecting_the_appended_event.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventAppendCollection.when_a_reactor_handles_an_event;

[Collection(ChronicleCollection.Name)]
public class and_collecting_the_appended_event(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : given.an_event_append_collection_scope(fixture)
    {
        async Task Because()
        {
            var reactor = EventStore.Reactors.GetHandlerFor<AReactor>();
            await reactor.WaitTillActive();

            AppendedEventsCollector = StartCollectingAppends();
            await EventStore.EventLog.Append(EventSourceId, new AnEventHappened(42));
        }
    }

    [Fact] void should_collect_one_event() => Context.AppendedEventsCollector.All.Count.ShouldEqual(1);
    [Fact] void should_have_appended_the_event() => Context.AppendedEventsCollector.All[0].Event.ShouldBeOfExactType<AnEventHappened>();
    [Fact] void should_be_successful() => Context.AppendedEventsCollector.All[0].IsSuccess.ShouldBeTrue();
    [Fact] void should_have_a_valid_sequence_number() => Context.AppendedEventsCollector.All[0].SequenceNumber.IsActualValue.ShouldBeTrue();
    [Fact] void should_be_for_the_correct_event_source() => Context.AppendedEventsCollector.All[0].EventSourceId.ShouldEqual(Context.EventSourceId);
}
