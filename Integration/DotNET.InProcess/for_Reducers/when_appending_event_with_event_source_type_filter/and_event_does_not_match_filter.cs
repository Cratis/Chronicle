// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.InProcess.Integration.for_Reducers.when_appending_event_with_event_source_type_filter.and_event_does_not_match_filter.context;

namespace Cratis.Chronicle.InProcess.Integration.for_Reducers.when_appending_event_with_event_source_type_filter;

[Collection(ChronicleCollection.Name)]
public class and_event_does_not_match_filter(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        static readonly TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public EventSourceId EventSourceId;
        public SomeEvent NonMatchingEvent;
        public SomeEvent MatchingEvent;
        public ReducerFilteredByEventSourceType Reducer;
        public ReducerState ReducerState;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reducers => [typeof(ReducerFilteredByEventSourceType)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reducer = new ReducerFilteredByEventSourceType(_tcs);
            services.AddSingleton(Reducer);
        }

        void Establish()
        {
            EventSourceId = "some-order-source";
            NonMatchingEvent = new SomeEvent(1);
            MatchingEvent = new SomeEvent(2);
        }

        async Task Because()
        {
            var reducer = EventStore.Reducers.GetHandlerFor<ReducerFilteredByEventSourceType>();
            await reducer.WaitTillActive();

            // Append an event with a non-matching event source type (default) — should be filtered out
            await EventStore.EventLog.Append(EventSourceId, NonMatchingEvent);

            // Append an event with the matching event source type — should be handled
            await EventStore.EventLog.Append(EventSourceId, MatchingEvent, eventSourceType: new Events.EventSourceType("order"));
            await _tcs.Task.WaitAsync(TimeSpanFactory.DefaultTimeout());
            await reducer.WaitTillReachesEventSequenceNumber(EventSequenceNumber.First.Next());
            ReducerState = await reducer.GetState();
        }
    }

    [Fact]
    void should_only_handle_the_matching_event() => Context.Reducer.HandledEvents.ShouldEqual(1);

    [Fact]
    void should_have_observer_state_be_active() => Context.ReducerState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_have_correct_last_handled_event_sequence_number() =>
        Context.ReducerState.LastHandledEventSequenceNumber.Value.ShouldEqual(1ul);
}
