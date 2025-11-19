// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.Specifications.for_Reducers.when_appending_event.and_waiting_for_observer_to_be_active.context;

namespace Cratis.Chronicle.Integration.Specifications.for_Reducers.when_appending_event;

[Collection(ChronicleCollection.Name)]
public class and_waiting_for_observer_to_be_active(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public static TaskCompletionSource Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public EventSourceId EventSourceId;
        public SomeEvent Event;
        public SomeReducer Reducer;
        public ReducerState ReducerState;
        public Exception WaitingForObserverStateError;
        public IEnumerable<FailedPartition> FailedPartitions;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reducers => [typeof(SomeReducer)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reducer = new SomeReducer(Tcs);
            services.AddSingleton(Reducer);
        }

        void Establish()
        {
            EventSourceId = "some source";
            Event = new SomeEvent(42);
        }

        async Task Because()
        {
            var reducer = EventStore.Reducers.GetHandlerFor<SomeReducer>();
            await reducer.WaitTillActive();
            await EventStore.EventLog.Append(EventSourceId, Event);
            await Tcs.Task.WaitAsync(TimeSpanFactory.DefaultTimeout());
            WaitingForObserverStateError = await Catch.Exception(async () => await EventStore.Reducers.WaitTillActive<SomeReducer>());
            await reducer.WaitTillReachesEventSequenceNumber(EventSequenceNumber.First);
            ReducerState = await reducer.GetState();

            FailedPartitions = await reducer.GetFailedPartitions();
        }
    }

    [Fact] Task should_have_correct_next_sequence_number() => Context.ShouldHaveNextSequenceNumber(1);

    [Fact] Task should_have_correct_tail_sequence_number() => Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);

    [Fact] void should_have_handled_the_event() => Context.Reducer.HandledEvents.ShouldEqual(1);

    [Fact]
    void should_not_fail_to_wait_for_observer_to_be_active_again() => Context.WaitingForObserverStateError.ShouldBeNull();

    [Fact]
    void should_have_observer_state_be_active() => Context.ReducerState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_have_correct_observer_state_last_handled_event_sequence_number() => Context.ReducerState.LastHandledEventSequenceNumber.Value.ShouldEqual(0ul);

    [Fact]
    void should_have_correct_observer_state_next_event_sequence_number() => Context.ReducerState.NextEventSequenceNumber.Value.ShouldEqual(1ul);

    [Fact]
    void should_not_have_failed_partitions() => Context.FailedPartitions.ShouldBeEmpty();
}
