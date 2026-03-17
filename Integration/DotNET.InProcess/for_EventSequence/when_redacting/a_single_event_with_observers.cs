// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_redacting.a_single_event_with_observers.context;
using KernelAppendedEvent = Cratis.Chronicle.Concepts.Events.AppendedEvent;
using KernelGlobalEventTypes = Cratis.Chronicle.Concepts.Events.GlobalEventTypes;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_redacting;

[Collection(ChronicleCollection.Name)]
public class a_single_event_with_observers(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId { get; } = "some source";
        public SomeEvent FirstEvent { get; private set; }
        public AnotherEvent SecondEvent { get; private set; }
        public SomeEvent ThirdEvent { get; private set; }
        public KernelAppendedEvent RedactedStoredEvent { get; private set; }
        public KernelAppendedEvent SystemStoredEvent { get; private set; }
        public IEnumerable<Job> ReplayJobs { get; private set; }
        public IEnumerable<Job> JobsAfterCompletion { get; private set; }
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
        }

        async Task Because()
        {
            var reactorHandler = EventStore.Reactors.GetHandlerFor<SomeReactor>();
            var reducerHandler = EventStore.Reducers.GetHandlerFor<SomeReducer>();
            var projectionHandler = EventStore.Projections.GetHandlerFor<SomeProjection>();

            await reactorHandler.WaitTillActive();
            await reducerHandler.WaitTillActive();
            await projectionHandler.WaitTillActive();

            await EventStore.EventLog.Append(EventSourceId, FirstEvent);
            await EventStore.EventLog.Append(EventSourceId, SecondEvent);
            await EventStore.EventLog.Append(EventSourceId, ThirdEvent);

            // Wait for all observers to process the 3 events.
            await Reactor.WaitTillHandledEventReaches(3);
            await Reducer.WaitTillHandledEventReaches(3);
            await projectionHandler.WaitTillReachesEventSequenceNumber(EventSequenceNumber.First + 2);

            // Reset counters before redaction to measure replay.
            Reactor.HandledEvents = 0;
            Reducer.HandledEvents = 0;

            // Redact the second event (sequence number 1).
            await this.RedactEvent(EventSequenceNumber.First + 1, "test reason");

            // Wait for replay jobs to appear and complete.
            ReplayJobs = await EventStore.Jobs.WaitForThereToBeJobs();
            await EventStore.Jobs.WaitForThereToBeNoJobs();

            // Wait for observers to finish processing the replay.
            await Reactor.WaitTillHandledEventReaches(2);
            await Reducer.WaitTillHandledEventReaches(2);

            JobsAfterCompletion = await EventStore.Jobs.GetJobs();

            var storage = GetEventLogStorage();
            RedactedStoredEvent = await storage.GetEventAt((EventSequenceNumber.First + 1).Value);
            var systemStorage = GetSystemEventLogStorage();
            var tailSequenceNumber = await systemStorage.GetTailSequenceNumber();
            SystemStoredEvent = await systemStorage.GetEventAt(tailSequenceNumber);

            ReactorState = await reactorHandler.GetState();
            ReducerState = await reducerHandler.GetState();
            ProjectionState = await projectionHandler.GetState();
        }
    }

    [Fact]
    void should_mark_event_as_redacted() => Context.RedactedStoredEvent.Context.EventType.Id.Value.ShouldEqual(KernelGlobalEventTypes.Redaction.Value);

    [Fact]
    void should_have_appended_system_event() => Context.SystemStoredEvent.Context.EventType.Id.Value.ShouldEqual("EventRedactionRequested");

    [Fact]
    void should_have_started_replay_jobs() => Context.ReplayJobs.ShouldNotBeEmpty();

    [Fact]
    void should_have_replay_jobs_of_correct_type() => Context.ReplayJobs.Any(j => j.Type.Value.Contains(nameof(ReplayObserverPartition))).ShouldBeTrue();

    [Fact]
    void should_have_replayed_reactor() => Context.Reactor.HandledEvents.ShouldBeGreaterThanOrEqual(2);

    [Fact]
    void should_have_replayed_reducer() => Context.Reducer.HandledEvents.ShouldBeGreaterThanOrEqual(2);

    [Fact]
    void should_have_reactor_in_active_state() => Context.ReactorState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_have_reducer_in_active_state() => Context.ReducerState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_have_projection_in_active_state() => Context.ProjectionState.RunningState.ShouldEqual(ObserverRunningState.Active);
}
