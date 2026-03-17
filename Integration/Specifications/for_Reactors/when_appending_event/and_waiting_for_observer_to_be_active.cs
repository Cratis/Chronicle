// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.Specifications.for_Reactors.when_appending_event.and_waiting_for_observer_to_be_active.context;

namespace Cratis.Chronicle.Integration.Specifications.for_Reactors.when_appending_event;

[Collection(ChronicleCollection.Name)]
public class and_waiting_for_observer_to_be_active(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public static TaskCompletionSource Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public EventSourceId EventSourceId;
        public SomeEvent Event;
        public SomeReactor Reactor;
        public ReactorState ReactorState;
        public Exception WaitingForObserverStateError;
        public IEnumerable<FailedPartition> FailedPartitions;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reactors => [typeof(SomeReactor)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reactor = new SomeReactor(Tcs);
            services.AddSingleton(Reactor);
        }

        void Establish()
        {
            EventSourceId = "some source";
            Event = new SomeEvent(42);
        }

        async Task Because()
        {
            var reactor = EventStore.Reactors.GetHandlerFor<SomeReactor>();
            await reactor.WaitTillActive();
            await EventStore.EventLog.Append(EventSourceId, Event);
            await Tcs.Task.WaitAsync(TimeSpanFactory.DefaultTimeout());
            WaitingForObserverStateError = await Catch.Exception(async () => await EventStore.Reactors.WaitForState<SomeReactor>(ObserverRunningState.Active));
            await reactor.WaitTillReachesEventSequenceNumber(EventSequenceNumber.First);
            ReactorState = await reactor.GetState();
            FailedPartitions = await reactor.GetFailedPartitions();
        }
    }

    [Fact] Task should_have_correct_next_sequence_number() => Context.ShouldHaveNextSequenceNumber(1);

    [Fact] Task should_have_correct_tail_sequence_number() => Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);

    [Fact] void should_have_handled_the_event() => Context.Reactor.HandledEvents.ShouldEqual(1);

    [Fact]
    void should_not_fail_to_wait_for_observer_to_be_active_again() => Context.WaitingForObserverStateError.ShouldBeNull();

    [Fact]
    void should_have_observer_state_be_active() => Context.ReactorState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_have_correct_observer_state_last_handled_event_sequence_number() => Context.ReactorState.LastHandledEventSequenceNumber.Value.ShouldEqual(0ul);

    [Fact]
    void should_have_correct_observer_state_next_event_sequence_number() => Context.ReactorState.NextEventSequenceNumber.Value.ShouldEqual(1ul);

    [Fact]
    void should_not_have_failed_partitions() => Context.FailedPartitions.ShouldBeEmpty();
}
