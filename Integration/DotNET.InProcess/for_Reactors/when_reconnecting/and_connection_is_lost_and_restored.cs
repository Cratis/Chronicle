// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.InProcess.Integration.for_Reactors.when_reconnecting.and_connection_is_lost_and_restored.context;

namespace Cratis.Chronicle.InProcess.Integration.for_Reactors.when_reconnecting;

[Collection(ChronicleCollection.Name)]
public class and_connection_is_lost_and_restored(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId;
        public ReactorWithoutDelay Reactor;
        public ReactorState ReactorState;
        public EventSequenceNumber SecondAppendSequenceNumber;
        public IEnumerable<FailedPartition> FailedPartitions = [];

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reactors => [typeof(ReactorWithoutDelay)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reactor = new();
            services.AddSingleton(Reactor);
        }

        void Establish() => EventSourceId = "reactor";

        async Task Because()
        {
            var reactor = EventStore.Reactors.GetHandlerFor<ReactorWithoutDelay>();
            await reactor.WaitTillActive();

            var firstAppendResult = await EventStore.EventLog.Append(EventSourceId, new SomeEvent(1));
            await reactor.WaitTillReachesEventSequenceNumber(firstAppendResult.SequenceNumber);
            await Reactor.WaitTillHandledEventReaches(1);

            await EventStore.Connection.Lifecycle.Disconnected();
            await EventStore.Connection.Connect();

            reactor = EventStore.Reactors.GetHandlerFor<ReactorWithoutDelay>();
            await reactor.WaitTillActive(TimeSpanFactory.FromSeconds(30));

            var secondAppendResult = await EventStore.EventLog.Append(EventSourceId, new SomeEvent(2));
            SecondAppendSequenceNumber = secondAppendResult.SequenceNumber;
            await reactor.WaitTillReachesEventSequenceNumber(SecondAppendSequenceNumber);
            await Reactor.WaitTillHandledEventReaches(2);

            ReactorState = await reactor.WaitTillActiveAndGetState(TimeSpanFactory.FromSeconds(30));
            FailedPartitions = await reactor.GetFailedPartitions();
        }
    }

    [Fact] Task should_have_two_events_in_the_event_log() => Context.ShouldHaveTailSequenceNumber(Context.SecondAppendSequenceNumber);

    [Fact] void should_continue_handling_events_after_reconnecting() => Context.Reactor.HandledEvents.ShouldEqual(2);

    [Fact] void should_have_reactor_observer_be_active() => Context.ReactorState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact] void should_have_reached_the_last_appended_event() => Context.ReactorState.LastHandledEventSequenceNumber.Value.ShouldEqual(Context.SecondAppendSequenceNumber.Value);

    [Fact] void should_not_have_failed_partitions() => Context.FailedPartitions.ShouldBeEmpty();
}
