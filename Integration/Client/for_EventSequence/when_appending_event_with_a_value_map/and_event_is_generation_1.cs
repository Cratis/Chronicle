// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending_event_with_a_value_map.and_event_is_generation_1.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_event_with_a_value_map;

[Collection(ChronicleCollection.Name)]
public class and_event_is_generation_1(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(SubscriptionStateChangedV1), typeof(SubscriptionStateChanged)];
        public override IEnumerable<Type> EventTypeMigrators => [typeof(SubscriptionStateChangedMigrator)];

        public EventSourceId EventSourceId { get; } = "some-subscription";
        public SubscriptionStateChangedV1 Event { get; private set; }
        public IAppendResult AppendResult { get; private set; }

        void Establish()
        {
            Event = new SubscriptionStateChangedV1(SubscriptionStateV1.Active);
        }

        async Task Because()
        {
            AppendResult = await EventStore.EventLog.Append(EventSourceId, Event);
        }
    }

    [Fact] void should_succeed() => Context.AppendResult.IsSuccess.ShouldBeTrue();
    [Fact] Task should_have_correct_tail_sequence_number() => Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);

    [Fact] Task should_have_mapped_the_state_into_generation_2_content() =>
        Context.ShouldHaveAppendedEvent<SubscriptionStateChanged>(0, Context.EventSourceId.Value, e => e.State.ShouldEqual(SubscriptionState.Running));
}
