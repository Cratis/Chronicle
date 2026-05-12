// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.Specifications.for_Reactors.when_appending_to_outbox.and_reactor_forwards_event_to_outbox.context;

namespace Cratis.Chronicle.Integration.Specifications.for_Reactors.when_appending_to_outbox;

[Collection(ChronicleCollection.Name)]
public class and_reactor_forwards_event_to_outbox(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification<ChronicleFixture>(chronicleInProcessFixture)
    {
        public static TaskCompletionSource Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public EventSourceId EventSourceId;
        public SomeEvent Event;
        public EventSequenceNumber OutboxTailSequenceNumber;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reactors => [typeof(OutboxForwardingReactor)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(Tcs);
        }

        void Establish()
        {
            EventSourceId = "outbox-test-source";
            Event = new SomeEvent(42);
        }

        async Task Because()
        {
            var reactor = EventStore.Reactors.GetHandlerFor<OutboxForwardingReactor>();
            await reactor.WaitTillActive();
            await EventStore.EventLog.Append(EventSourceId, Event);
            await Tcs.Task.WaitAsync(TimeSpanFactory.DefaultTimeout());
            OutboxTailSequenceNumber = await EventStore.GetEventSequence(EventSequenceId.Outbox).GetTailSequenceNumber();
        }
    }

    [Fact] Task should_have_only_one_event_in_event_log() => Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);

    [Fact] void should_have_event_in_outbox() => Context.OutboxTailSequenceNumber.ShouldEqual(EventSequenceNumber.First);
}
