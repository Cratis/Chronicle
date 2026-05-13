// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_EventAppendCollection.when_a_reactor_directly_appends_unique_events.and_the_constraint_is_satisfied.context;

namespace Cratis.Chronicle.Integration.for_EventAppendCollection.when_a_reactor_directly_appends_unique_events;

[Collection(ChronicleCollection.Name)]
public class and_the_constraint_is_satisfied(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_reactor_that_directly_appends_unique_events_scope(fixture)
    {
        public string UniqueValue;

        async Task Because()
        {
            var reactor = EventStore.Reactors.GetHandlerFor<AReactorThatDirectlyAppendsUniqueEvent>();
            await reactor.WaitTillActive();

            UniqueValue = Guid.NewGuid().ToString();
            var firstEventSourceId = EventSourceId.New();
            var secondEventSourceId = EventSourceId.New();

            AppendedEventsCollector = StartCollectingAppends();
            await EventStore.EventLog.Append(firstEventSourceId, new ADirectUniqueEvent(UniqueValue));
            await EventStore.EventLog.Append(secondEventSourceId, new ADirectUniqueEvent(UniqueValue));
            await AppendedEventsCollector.WaitForCount(4);
        }
    }

    AppendedEventWithResult FirstFollowUp => Context.AppendedEventsCollector.All.First(e => e.Event.Content is ADirectUniqueFollowUpEvent);

    [Fact] void should_be_successful() => FirstFollowUp.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_have_constraint_violations() => FirstFollowUp.Result.HasConstraintViolations.ShouldBeFalse();
    [Fact] void should_have_appended_the_follow_up_event() => FirstFollowUp.Event.Content.ShouldBeOfExactType<ADirectUniqueFollowUpEvent>();
    [Fact] void should_carry_the_unique_value() => ((ADirectUniqueFollowUpEvent)FirstFollowUp.Event.Content).UniqueValue.ShouldEqual(Context.UniqueValue);
}
