// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_redacting.a_single_event_with_non_replayable_observers.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_redacting;

[Collection(ChronicleCollection.Name)]
public class a_single_event_with_non_replayable_observers(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId { get; } = "some source";
        public SomeEvent FirstEvent { get; private set; }
        public AnotherEvent SecondEvent { get; private set; }
        public SomeEvent ThirdEvent { get; private set; }
        public ReactorState ReactorState { get; private set; }
        public ReducerState ReducerState { get; private set; }
        public SomeReactor Reactor { get; private set; }
        public SomeReducer Reducer { get; private set; }
        public NonReplayableReactor NonReplayableReactorInstance { get; private set; }
        public NonReplayableReducer NonReplayableReducerInstance { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent), typeof(AnotherEvent)];
        public override IEnumerable<Type> Reactors => [typeof(SomeReactor), typeof(NonReplayableReactor)];
        public override IEnumerable<Type> Reducers => [typeof(SomeReducer), typeof(NonReplayableReducer)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reactor = new SomeReactor();
            Reducer = new SomeReducer();
            NonReplayableReactorInstance = new NonReplayableReactor();
            NonReplayableReducerInstance = new NonReplayableReducer();
            services.AddSingleton(Reactor);
            services.AddSingleton(Reducer);
            services.AddSingleton(NonReplayableReactorInstance);
            services.AddSingleton(NonReplayableReducerInstance);
        }

        void Establish()
        {
            FirstEvent = new SomeEvent("first content");
            SecondEvent = new AnotherEvent(42);
            ThirdEvent = new SomeEvent("third content");
        }

        async Task Because()
        {
            var reactorHandler = EventStore.Reactors.GetHandlerFor<SomeReactor>();
            var reducerHandler = EventStore.Reducers.GetHandlerFor<SomeReducer>();
            var nonReplayableReactorHandler = EventStore.Reactors.GetHandlerFor<NonReplayableReactor>();
            var nonReplayableReducerHandler = EventStore.Reducers.GetHandlerFor<NonReplayableReducer>();

            await reactorHandler.WaitTillActive();
            await reducerHandler.WaitTillActive();
            await nonReplayableReactorHandler.WaitTillActive();
            await nonReplayableReducerHandler.WaitTillActive();

            await EventStore.EventLog.Append(EventSourceId, FirstEvent);
            await EventStore.EventLog.Append(EventSourceId, SecondEvent);
            await EventStore.EventLog.Append(EventSourceId, ThirdEvent);

            // Wait for all observers to process the 3 events.
            await Reactor.WaitTillHandledEventReaches(3);
            await Reducer.WaitTillHandledEventReaches(3);
            await NonReplayableReactorInstance.WaitTillHandledEventReaches(3);
            await NonReplayableReducerInstance.WaitTillHandledEventReaches(3);

            // Mark the non-replayable observers as non-replayable in storage.
            await MarkObserverAsNonReplayable(typeof(NonReplayableReactor).GetReactorId());
            await MarkObserverAsNonReplayable(typeof(NonReplayableReducer).GetReducerId().Value);

            // Reset counters before redaction to measure replay.
            Reactor.HandledEvents = 0;
            Reducer.HandledEvents = 0;
            NonReplayableReactorInstance.HandledEvents = 0;
            NonReplayableReducerInstance.HandledEvents = 0;

            // Redact the second event (sequence number 1).
            await this.RedactEvent(EventSequenceNumber.First + 1, "test reason");

            // Wait for replay jobs to complete.
            await EventStore.Jobs.WaitForThereToBeNoJobs();

            // Wait for replayable observers to finish processing the replay.
            await Reactor.WaitTillHandledEventReaches(2);
            await Reducer.WaitTillHandledEventReaches(2);

            // Give a brief window for non-replayable observers in case they would incorrectly be replayed.
            await Task.Delay(500);

            ReactorState = await reactorHandler.GetState();
            ReducerState = await reducerHandler.GetState();
        }

        async Task MarkObserverAsNonReplayable(string observerId)
        {
            var observerDefinitions = EventStoreStorage.Observers;
            var definition = await observerDefinitions.Get(observerId);
            var updated = definition with { IsReplayable = false };
            await observerDefinitions.Save(updated);
        }
    }

    [Fact]
    void should_have_replayed_reactor() => Context.Reactor.HandledEvents.ShouldBeGreaterThanOrEqual(2);

    [Fact]
    void should_have_replayed_reducer() => Context.Reducer.HandledEvents.ShouldBeGreaterThanOrEqual(2);

    [Fact]
    void should_not_have_replayed_non_replayable_reactor() => Context.NonReplayableReactorInstance.HandledEvents.ShouldEqual(0);

    [Fact]
    void should_not_have_replayed_non_replayable_reducer() => Context.NonReplayableReducerInstance.HandledEvents.ShouldEqual(0);

    [Fact]
    void should_have_reactor_in_active_state() => Context.ReactorState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_have_reducer_in_active_state() => Context.ReducerState.RunningState.ShouldEqual(ObserverRunningState.Active);
}
