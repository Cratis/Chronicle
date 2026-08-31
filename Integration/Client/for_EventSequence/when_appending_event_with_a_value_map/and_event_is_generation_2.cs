// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending_event_with_a_value_map.and_event_is_generation_2.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_event_with_a_value_map;

/// <summary>
/// The map is present in both directions, so an event written at the newer generation goes through the inverted one
/// on its way to being stored at the older generation. What it must not do is come back out of the log translated -
/// an append at the current generation has nothing to translate.
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_event_is_generation_2(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(SubscriptionStateChangedV1), typeof(SubscriptionStateChanged)];
        public override IEnumerable<Type> EventTypeMigrators => [typeof(SubscriptionStateChangedMigrator)];

        public EventSourceId EventSourceId { get; } = "another-subscription";
        public SubscriptionStateChanged Event { get; private set; }
        public IAppendResult AppendResult { get; private set; }

        void Establish()
        {
            Event = new SubscriptionStateChanged(SubscriptionState.Stopped);
        }

        async Task Because()
        {
            AppendResult = await EventStore.EventLog.Append(EventSourceId, Event);
        }
    }

    [Fact] void should_succeed() => Context.AppendResult.IsSuccess.ShouldBeTrue();
    [Fact] Task should_have_correct_tail_sequence_number() => Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);

    [Fact] Task should_have_left_the_state_alone_at_the_generation_it_was_written_at() =>
        Context.ShouldHaveAppendedEvent<SubscriptionStateChanged>(0, Context.EventSourceId.Value, e => e.State.ShouldEqual(SubscriptionState.Stopped));
}
