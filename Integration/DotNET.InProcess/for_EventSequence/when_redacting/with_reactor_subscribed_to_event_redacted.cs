// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_redacting.with_reactor_subscribed_to_event_redacted.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_redacting;

[Collection(ChronicleCollection.Name)]
public class with_reactor_subscribed_to_event_redacted(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId { get; } = "some source";
        public SomeEvent SomeEventInstance { get; private set; }
        public AnotherEvent AnotherEventInstance { get; private set; }
        public ReactorSubscribedToEventRedacted Reactor { get; private set; }
        public ReactorState ReactorState { get; private set; }
        public EventSequenceNumber SomeEventSequenceNumber { get; private set; }
        public EventSequenceNumber AnotherEventSequenceNumber { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent), typeof(AnotherEvent)];
        public override IEnumerable<Type> Reactors => [typeof(ReactorSubscribedToEventRedacted)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reactor = new ReactorSubscribedToEventRedacted();
            services.AddSingleton(Reactor);
        }

        void Establish()
        {
            SomeEventInstance = new SomeEvent("content");
            AnotherEventInstance = new AnotherEvent(42);
        }

        async Task Because()
        {
            var startupTimeout = TimeSpanFactory.FromSeconds(30);
            var reactorHandler = EventStore.Reactors.GetHandlerFor<ReactorSubscribedToEventRedacted>();

            await reactorHandler.WaitTillActive(startupTimeout);

            // Append both events
            var someEventResult = await EventStore.EventLog.Append(EventSourceId, SomeEventInstance);
            var anotherEventResult = await EventStore.EventLog.Append(EventSourceId, AnotherEventInstance);
            SomeEventSequenceNumber = someEventResult.SequenceNumber;
            AnotherEventSequenceNumber = anotherEventResult.SequenceNumber;

            // Wait for the reactor to process all events
            await reactorHandler.WaitTillReachesEventSequenceNumber(AnotherEventSequenceNumber, startupTimeout);

            // Reset counters before redactions
            Reactor.HandledSomeEvents = 0;
            Reactor.HandledEventRedactedEvents = 0;

            // First redact AnotherEvent (which the reactor does NOT subscribe to)
            // This creates an EventRedacted event that the reactor should NOT see during its own replay
            await this.RedactEvent(AnotherEventSequenceNumber, "redacting another event");

            // Wait for replay jobs (for other observers of AnotherEvent) to complete
            await EventStore.Jobs.WaitForThereToBeNoJobs();

            // Now redact SomeEvent (which the reactor DOES subscribe to)
            // This will cause the reactor to replay, and it should ONLY see EventRedacted for SomeEvent
            await this.RedactEvent(SomeEventSequenceNumber, "redacting some event");

            // Wait for replay jobs to complete
            await EventStore.Jobs.WaitForThereToBeNoJobs();

            // Wait for the reactor to process the EventRedacted event
            await Reactor.WaitTillEventRedactedHandledReaches(1, startupTimeout);

            ReactorState = await reactorHandler.GetState();
        }
    }

    [Fact]
    void should_have_reactor_in_active_state() => Context.ReactorState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_have_received_exactly_one_event_redacted() => Context.Reactor.HandledEventRedactedEvents.ShouldEqual(1);

    [Fact]
    void should_have_received_event_redacted_with_original_type_of_some_event() =>
        Context.Reactor.RedactedOriginalTypes[0].ShouldEqual(typeof(SomeEvent));
}
