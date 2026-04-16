// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.InProcess.Integration.for_Reactors.when_appending_event_with_event_stream_type_filter.and_event_does_not_match_filter.context;

namespace Cratis.Chronicle.InProcess.Integration.for_Reactors.when_appending_event_with_event_stream_type_filter;

[Collection(ChronicleCollection.Name)]
public class and_event_does_not_match_filter(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        static readonly TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public EventSourceId EventSourceId;
        public SomeEvent NonMatchingEvent;
        public SomeEvent MatchingEvent;
        public ReactorFilteredByEventStreamType Reactor;
        public ReactorState ReactorState;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reactors => [typeof(ReactorFilteredByEventStreamType)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reactor = new ReactorFilteredByEventStreamType(_tcs);
            services.AddSingleton(Reactor);
        }

        void Establish()
        {
            EventSourceId = "some-order-source";
            NonMatchingEvent = new SomeEvent(1);
            MatchingEvent = new SomeEvent(2);
        }

        async Task Because()
        {
            var reactor = EventStore.Reactors.GetHandlerFor<ReactorFilteredByEventStreamType>();
            await reactor.WaitTillActive();

            // Append an event with a non-matching event stream type (default "All") — should be filtered out
            await EventStore.EventLog.Append(EventSourceId, NonMatchingEvent);

            // Append an event with the matching event stream type — should be handled
            await EventStore.EventLog.Append(EventSourceId, MatchingEvent, eventStreamType: new EventStreamType("orders"));
            await _tcs.Task.WaitAsync(TimeSpanFactory.DefaultTimeout());
            await reactor.WaitTillReachesEventSequenceNumber(EventSequenceNumber.First.Next());
            ReactorState = await reactor.GetState();
        }
    }

    [Fact]
    void should_only_handle_the_matching_event() => Context.Reactor.HandledEvents.ShouldEqual(1);

    [Fact]
    void should_have_observer_state_be_active() => Context.ReactorState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_have_correct_last_handled_event_sequence_number() =>
        Context.ReactorState.LastHandledEventSequenceNumber.Value.ShouldEqual(1ul);
}
