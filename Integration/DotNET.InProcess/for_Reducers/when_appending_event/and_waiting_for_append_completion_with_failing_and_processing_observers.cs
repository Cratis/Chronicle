// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.InProcess.Integration.for_Reducers.when_appending_event.and_waiting_for_append_completion_with_failing_and_processing_observers.context;

namespace Cratis.Chronicle.InProcess.Integration.for_Reducers.when_appending_event;

[Collection(ChronicleCollection.Name)]
public class and_waiting_for_append_completion_with_failing_and_processing_observers(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId;
        public SomeEvent Event;
        public FailingReactorThatCanFailOnce FailingReactor;
        public SlowReducerThatSignalsCompletion SlowReducer;
        public ReactorState FailingReactorState;
        public ReducerState SlowReducerState;
        public IEnumerable<FailedPartition> FailedPartitionsForFailingReactor;
        public IEnumerable<FailedPartition> FailedPartitionsForSlowReducer;
        public bool SlowReducerCompletedWhileFailingReactorHasFailedPartition;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reducers => [typeof(SlowReducerThatSignalsCompletion)];
        public override IEnumerable<Type> Reactors => [typeof(FailingReactorThatCanFailOnce)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            FailingReactor = new FailingReactorThatCanFailOnce
            {
                ShouldFail = true
            };
            SlowReducer = new SlowReducerThatSignalsCompletion(new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
            {
                HandleTime = TimeSpan.FromMilliseconds(750)
            };
            services.AddSingleton(FailingReactor);
            services.AddSingleton(SlowReducer);
        }

        void Establish()
        {
            EventSourceId = "some source";
            Event = new SomeEvent(42);
        }

        async Task Because()
        {
            var timeout = TimeSpanFactory.FromSeconds(30);
            var failingReactor = EventStore.Reactors.GetHandlerFor<FailingReactorThatCanFailOnce>();
            var slowReducer = EventStore.Reducers.GetHandlerFor<SlowReducerThatSignalsCompletion>();
            await failingReactor.WaitTillActive(timeout);
            await slowReducer.WaitTillActive(timeout);

            await EventStore.EventLog.Append(EventSourceId, Event);
            FailedPartitionsForFailingReactor = await failingReactor.WaitForThereToBeFailedPartitions();
            await SlowReducer.Completion.WaitAsync(timeout);
            SlowReducerCompletedWhileFailingReactorHasFailedPartition = SlowReducer.Completion.IsCompleted;

            FailingReactorState = await failingReactor.GetState();
            SlowReducerState = await slowReducer.WaitTillActiveAndGetState(timeout);
            FailedPartitionsForSlowReducer = await slowReducer.GetFailedPartitions();
        }
    }

    [Fact] void should_have_failed_partitions_for_failing_reactor() => Context.FailedPartitionsForFailingReactor.Count().ShouldEqual(1);
    [Fact] void should_allow_processing_observer_to_complete() => Context.SlowReducerCompletedWhileFailingReactorHasFailedPartition.ShouldBeTrue();
    [Fact] void should_have_processed_event_on_slow_reducer() => Context.SlowReducer.HandledEvents.ShouldEqual(1);
    [Fact] void should_not_have_failed_partitions_for_slow_reducer() => Context.FailedPartitionsForSlowReducer.ShouldBeEmpty();
    [Fact] void should_keep_failing_reactor_active() => Context.FailingReactorState.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_keep_slow_reducer_active() => Context.SlowReducerState.RunningState.ShouldEqual(ObserverRunningState.Active);
}
