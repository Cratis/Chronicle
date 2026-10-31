// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.for_Reducers.when_handling_event.and_it_fails.but_not_third_time.context;

namespace Cratis.Chronicle.Integration.for_Reducers.when_handling_event.and_it_fails;

[Collection(ChronicleCollection.Name)]
public class but_not_third_time(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_reducer_observing_an_event_that_can_fail(chronicleFixture, 3)
    {
        public IEnumerable<FailedPartition> FailedPartitionsBeforeRetry;
        public IEnumerable<FailedPartition> FailedPartitionsAfterRetry;
        public int HandlerAttempts;
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
            var startupTimeout = TimeSpanFactory.FromSeconds(30);

            var reducer = EventStore.Reducers.GetHandlerFor<ReducerThatCanFail>();
            await reducer.WaitTillSubscribed(startupTimeout);
            Observers[0].ShouldFail = true;
            Observers[1].ShouldFail = true;
            Observers[2].ShouldFail = false;
            await EventStore.EventLog.Append(EventSourceId, Event);

            // Wait for the first event to have been handled
            await Tcs[0].Task.WaitAsync(RetryWaits.Timeout);

            FailedPartitionsBeforeRetry = await reducer.WaitForThereToBeFailedPartitions(RetryWaits.Timeout);

            // Wait for the second event to have been handled
            await Tcs[1].Task.WaitAsync(RetryWaits.Timeout);

            // Wait for the third event to have been handled
            await Tcs[2].Task.WaitAsync(RetryWaits.Timeout);
            HandlerAttempts = Tcs.Count(_ => _.Task.IsCompleted);

            await Observers[2].WaitTillHandledEventReaches(1, RetryWaits.Timeout);

            // The handler succeeding is seen by the client before the kernel records the recovery, so wait for the kernel.
            await reducer.WaitTillReachesEventSequenceNumber(EventSequenceNumber.First, RetryWaits.Timeout);
            await RetryWaits.Until(async () => !(await reducer.GetFailedPartitions()).Any());
            FailedPartitionsAfterRetry = await reducer.GetFailedPartitions();

            ReducerState = await reducer.WaitTillActiveAndGetState(RetryWaits.Timeout);
        }
    }

    [Fact] void should_fail_one_partition() => Context.FailedPartitionsBeforeRetry.Count().ShouldEqual(1);
    [Fact] void should_retry_the_failed_partition() => Context.HandlerAttempts.ShouldEqual(3);
    [Fact] void should_recover_failed_partition() => Context.FailedPartitionsAfterRetry.ShouldBeEmpty();
    [Fact] void should_have_the_active_observer_running_state() => Context.ReducerState.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_have_correct_last_handled_event_sequence_number() => Context.ReducerState.LastHandledEventSequenceNumber.Value.ShouldEqual(0ul);
    [Fact] void should_have_correct_next_event_sequence_number() => Context.ReducerState.NextEventSequenceNumber.Value.ShouldEqual(1ul);
}
