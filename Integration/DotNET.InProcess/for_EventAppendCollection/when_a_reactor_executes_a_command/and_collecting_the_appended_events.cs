// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.InProcess.Integration.for_EventAppendCollection.when_a_reactor_executes_a_command.and_collecting_the_appended_events.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventAppendCollection.when_a_reactor_executes_a_command;

[Collection(ChronicleCollection.Name)]
public class and_collecting_the_appended_events(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : given.a_reactor_that_executes_a_command_scope(fixture)
    {
        async Task Because()
        {
            var reactor = EventStore.Reactors.GetHandlerFor<AReactorThatExecutesACommand>();
            await reactor.WaitTillActive();

            AppendedEventsCollector = StartCollectingAppends();
            await EventStore.EventLog.Append(EventSourceId, new AnEventHappened(42));
            await AppendedEventsCollector.WaitForCount(2);
        }
    }

    AppendedEventWithResult CommandHandledEvent => Context.AppendedEventsCollector.All.First(e => e.Event.Content is ACommandHandledEvent);

    [Fact] void should_collect_two_events() => Context.AppendedEventsCollector.All.Count.ShouldEqual(2);
    [Fact] void should_be_successful() => CommandHandledEvent.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_a_valid_sequence_number() => CommandHandledEvent.Result.SequenceNumber.IsActualValue.ShouldBeTrue();
    [Fact] void should_be_for_the_correct_event_source() => CommandHandledEvent.Event.Context.EventSourceId.ShouldEqual(Context.EventSourceId);
}
