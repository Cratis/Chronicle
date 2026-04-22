// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_revising.an_event_with_observers.context;
using KernelAppendedEvent = Cratis.Chronicle.Concepts.Events.AppendedEvent;
using KernelEventHash = Cratis.Chronicle.Concepts.Events.EventHash;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_revising;

[Collection(ChronicleCollection.Name)]
public class an_event_with_observers(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId { get; } = "some source";
        public SomeEvent FirstEvent { get; private set; }
        public AnotherEvent SecondEvent { get; private set; }
        public SomeEvent ThirdEvent { get; private set; }
        public SomeEvent RevisedEvent { get; private set; }
        public KernelAppendedEvent StoredEvent { get; private set; }
        public KernelAppendedEvent SystemStoredEvent { get; private set; }
        public ReactorState ReactorState { get; private set; }
        public ReducerState ReducerState { get; private set; }
        public ProjectionState ProjectionState { get; private set; }
        public SomeReactor Reactor { get; private set; }
        public SomeReducer Reducer { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent), typeof(AnotherEvent)];
        public override IEnumerable<Type> Reactors => [typeof(SomeReactor)];
        public override IEnumerable<Type> Reducers => [typeof(SomeReducer)];
        public override IEnumerable<Type> Projections => [typeof(SomeProjection)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reactor = new SomeReactor();
            Reducer = new SomeReducer();
            services.AddSingleton(Reactor);
            services.AddSingleton(Reducer);
        }

        void Establish()
        {
            FirstEvent = new SomeEvent("first content");
            SecondEvent = new AnotherEvent(42);
            ThirdEvent = new SomeEvent("third content");
            RevisedEvent = new SomeEvent("revised content");
        }

        async Task Because()
        {
            var startupTimeout = TimeSpanFactory.FromSeconds(30);
            var reactorHandler = EventStore.Reactors.GetHandlerFor<SomeReactor>();
            var reducerHandler = EventStore.Reducers.GetHandlerFor<SomeReducer>();
            var projectionHandler = EventStore.Projections.GetHandlerFor<SomeProjection>();

            await reactorHandler.WaitTillActive(startupTimeout);
            await reducerHandler.WaitTillActive(startupTimeout);
            await projectionHandler.WaitTillActive(startupTimeout);

            await EventStore.EventLog.Append(EventSourceId, FirstEvent);
            await EventStore.EventLog.Append(EventSourceId, SecondEvent);
            var lastAppendResult = await EventStore.EventLog.Append(EventSourceId, ThirdEvent);

            var lastAppendedSequenceNumber = lastAppendResult.SequenceNumber;

            // Wait for all observers to process the appended events.
            await reactorHandler.WaitTillReachesEventSequenceNumber(lastAppendedSequenceNumber, startupTimeout);
            await reducerHandler.WaitTillReachesEventSequenceNumber(lastAppendedSequenceNumber, startupTimeout);
            await projectionHandler.WaitTillReachesEventSequenceNumber(lastAppendedSequenceNumber, startupTimeout);

            // Reset counters before revision to measure replay.
            Reactor.HandledEvents = 0;
            Reducer.HandledEvents = 0;

            // Revise the first event (sequence number 0).
            await this.ReviseEvent(EventSequenceNumber.First, RevisedEvent);

            // Wait for replay jobs to complete.
            await EventStore.Jobs.WaitForThereToBeNoJobs();

            // Wait for observers to finish processing the replay.
            await Reactor.WaitTillHandledEventReaches(3, startupTimeout);
            await Reducer.WaitTillHandledEventReaches(3, startupTimeout);

            StoredEvent = await GetEventLogStorage().GetEventAt(EventSequenceNumber.First.Value);
            var systemStorage = GetSystemEventLogStorage();
            var tailSequenceNumber = await systemStorage.GetTailSequenceNumber();
            SystemStoredEvent = await systemStorage.GetEventAt(tailSequenceNumber);

            ReactorState = await reactorHandler.GetState();
            ReducerState = await reducerHandler.GetState();
            ProjectionState = await projectionHandler.GetState();
        }
    }

    [Fact]
    Task should_have_updated_content() => Context.ShouldHaveAppendedEvent<SomeEvent>(EventSequenceNumber.First, Context.EventSourceId, e => e.Content.ShouldEqual(Context.RevisedEvent.Content));

    [Fact]
    void should_have_a_hash_set() => Context.StoredEvent.Context.Hash.ShouldNotEqual(KernelEventHash.NotSet);

    [Fact]
    void should_have_appended_event_revised_to_system_log() => Context.SystemStoredEvent.Context.EventType.Id.Value.ShouldEqual("EventRevised");

    [Fact]
    void should_have_replayed_reactor() => Context.Reactor.HandledEvents.ShouldBeGreaterThanOrEqual(3);

    [Fact]
    void should_have_replayed_reducer() => Context.Reducer.HandledEvents.ShouldBeGreaterThanOrEqual(3);

    [Fact]
    void should_have_reactor_in_active_state() => Context.ReactorState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_have_reducer_in_active_state() => Context.ReducerState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_have_projection_in_active_state() => Context.ProjectionState.RunningState.ShouldEqual(ObserverRunningState.Active);
}
