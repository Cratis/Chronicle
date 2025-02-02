// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Observation;
using Cratis.Chronicle.Storage.Observation;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_appending_event.and_waiting_for_observer_to_be_active.context;
using ObserverRunningState = Cratis.Chronicle.Concepts.Observation.ObserverRunningState;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_appending_event;

[Collection(GlobalCollection.Name)]
public class and_waiting_for_observer_to_be_active(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : IntegrationSpecificationContext(globalFixture)
    {
        public static TaskCompletionSource Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public EventSourceId EventSourceId;
        public SomeEvent Event;
        public SomeReactor Reactor;
        public IObserver ReactorObserver;
        public ObserverState ReactorObserverState;
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
            ReactorObserver = GetObserverForReactor<SomeReactor>();
        }

        async Task Because()
        {
            await ReactorObserver.WaitTillActive();
            await EventStore.EventLog.Append(EventSourceId, Event);
            await Tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            WaitingForObserverStateError = await Catch.Exception(async () => await ReactorObserver.WaitForState(ObserverRunningState.Active, TimeSpan.FromSeconds(5)));
            ReactorObserverState = await ReactorObserver.GetState();

            FailedPartitions = await ServicesAccessor.Services.FailedPartitions.GetFailedPartitions(new()
            {
                EventStore = EventStore.Name.Value,
                Namespace = Concepts.EventStoreNamespaceName.Default,
                ObserverId = ReactorObserverState.Id
            });
        }
    }

    [Fact] Task should_have_correct_next_sequence_number() => Context.ShouldHaveCorrectNextSequenceNumber(1);

    [Fact] Task should_have_correct_tail_sequence_number() => Context.ShouldHaveCorrectTailSequenceNumber(Concepts.Events.EventSequenceNumber.First);

    [Fact] void should_have_handled_the_event() => Context.Reactor.HandledEvents.ShouldEqual(1);

    [Fact]
    void should_not_fail_to_wait_for_observer_to_be_active_again() => Context.WaitingForObserverStateError.ShouldBeNull();

    [Fact]
    void should_have_observer_state_be_active() => Context.ReactorObserverState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_have_correct_observer_state_last_handled_event_sequence_number() => Context.ReactorObserverState.LastHandledEventSequenceNumber.Value.ShouldEqual(0ul);

    [Fact]
    void should_have_correct_observer_state_next_event_sequence_number() => Context.ReactorObserverState.NextEventSequenceNumber.Value.ShouldEqual(1ul);

    [Fact]
    void should_not_have_failed_partitions() => Context.FailedPartitions.ShouldBeEmpty();
}
