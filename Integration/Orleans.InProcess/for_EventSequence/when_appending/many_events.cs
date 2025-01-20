// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Base;
using Xunit.Abstractions;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_EventSequence.when_appending.many_events.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_EventSequence.when_appending;

[Collection(GlobalCollection.Name)]
public class many_events(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : IntegrationSpecificationContext(globalFixture)
    {
        public Events.EventSourceId EventSourceId { get; } = "source";
        public IList<SomeEvent> Events { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        void Establish()
        {
            Events = [new SomeEvent("some value"), new SomeEvent("some other value"), new SomeEvent("some third value")];
        }

        async Task Because()
        {
            await EventStore.EventLog.AppendMany(EventSourceId, Events);
        }
    }

    [Fact] Task should_have_correct_next_sequence_number() => Context.ShouldHaveCorrectNextSequenceNumber((ulong)Context.Events.Count);

    [Fact] Task should_have_correct_tail_sequence_number() => Context.ShouldHaveCorrectTailSequenceNumber((ulong)Context.Events.Count - 1);

    [Fact]
    async Task should_have_stored_all_the_events_in_correct_order()
    {
        foreach (var (e, i) in Context.Events.Select((item, index) => (item, index)))
        {
            await Context.ShouldHaveStoredCorrectEvent<SomeEvent>((ulong)i, Context.EventSourceId.Value, (someEvent) => someEvent.Content.ShouldEqual(Context.Events[i].Content));
        }
    }
}
