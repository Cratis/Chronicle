// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_Reducers.when_handling_event.and_it_fails.but_not_second_time.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reducers.when_handling_event.and_it_fails;

[Collection(GlobalCollection.Name)]
public class but_not_second_time(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.a_reducer_observing_an_event_that_can_fail(globalFixture, 2)
    {
        public IEnumerable<FailedPartition> FailedPartitionsBeforeRetry;
        public IEnumerable<FailedPartition> FailedPartitionsAfterRetry;
        public IEnumerable<Job> Jobs;
        public SomeEvent Event;
        public EventSourceId EventSourceId;
        public ReducerState ReducerState;

        void Establish()
        {
            EventSourceId = "some source";
            Event = new SomeEvent(42);
        }

        async Task Because()
        {
            var reducer = EventStore.Reducers.GetHandlerFor<ReducerThatCanFail>();
            await reducer.WaitTillActive();
            Observers[0].ShouldFail = true;
            Observers[1].ShouldFail = false;
            await EventStore.EventLog.Append(EventSourceId, Event);

            // Wait for the first event to have been handled
            await Tcs[0].Task.WaitAsync(TimeSpanFactory.DefaultTimeout());

            FailedPartitionsBeforeRetry = await reducer.WaitForThereToBeFailedPartitions();
            Jobs = await EventStore.Jobs.WaitForThereToBeJobs();

            // Wait for the second event to have been handled
            await Tcs[1].Task.WaitAsync(TimeSpanFactory.DefaultTimeout());
            await EventStore.Jobs.WaitForThereToBeNoJobs();

            FailedPartitionsAfterRetry = await reducer.GetFailedPartitions();
            ReducerState = await reducer.GetState();
            await Observers[1].WaitTillHandledEventReaches(1);
        }
    }

    [Fact] void should_fail_one_partition() => Context.FailedPartitionsBeforeRetry.Count().ShouldEqual(1);
    [Fact] void should_start_replaying_job() => Context.Jobs.First().Type.Value.ShouldContain("RetryFailedPartition");
    [Fact] void should_recover_failed_partition() => Context.FailedPartitionsAfterRetry.ShouldBeEmpty();
    [Fact] void should_have_the_active_observer_running_state() => Context.ReducerState.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_have_correct_last_handled_event_sequence_number() => Context.ReducerState.LastHandledEventSequenceNumber.Value.ShouldEqual(0ul);
    [Fact] void should_have_correct_next_event_sequence_number() => Context.ReducerState.NextEventSequenceNumber.Value.ShouldEqual(1ul);
}
