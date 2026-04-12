// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.InProcess.Integration.for_Reducers.when_handling_event.and_it_fails.but_not_third_time.context;

namespace Cratis.Chronicle.InProcess.Integration.for_Reducers.when_handling_event.and_it_fails;

[Collection(ChronicleCollection.Name)]
public class but_not_third_time(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_reducer_observing_an_event_that_can_fail(chronicleInProcessFixture, 3)
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
            // The retry backoff uses exponential delay: 2s for the first retry, 4s for the second.
            // The default 5s timeout is too tight for the second cycle, so use a longer timeout.
            var retryTimeout = TimeSpan.FromSeconds(10);
            var startupTimeout = TimeSpanFactory.FromSeconds(30);

            var reducer = EventStore.Reducers.GetHandlerFor<ReducerThatCanFail>();
            await reducer.WaitTillActive(startupTimeout);
            Observers[0].ShouldFail = true;
            Observers[1].ShouldFail = true;
            Observers[2].ShouldFail = false;
            await EventStore.EventLog.Append(EventSourceId, Event);

            // Wait for the first event to have been handled
            await Tcs[0].Task.WaitAsync(retryTimeout);

            FailedPartitionsBeforeRetry = await reducer.WaitForThereToBeFailedPartitions();
            Jobs = await EventStore.Jobs.WaitForThereToBeJobOfType(nameof(RetryFailedPartition), retryTimeout);

            // Wait for the second event to have been handled
            await Tcs[1].Task.WaitAsync(retryTimeout);

            FailedPartitionsBeforeRetry = await reducer.WaitForThereToBeFailedPartitions();
            Jobs = await EventStore.Jobs.WaitForThereToBeJobOfType(nameof(RetryFailedPartition), retryTimeout);

            // Wait for the third event to have been handled
            await Tcs[2].Task.WaitAsync(retryTimeout);
            await EventStore.Jobs.WaitForThereToBeNoJobs(retryTimeout, JobStatus.CompletedWithFailures);

            FailedPartitionsAfterRetry = await reducer.GetFailedPartitions();
            await Observers[2].WaitTillHandledEventReaches(1);

            ReducerState = await reducer.WaitTillActiveAndGetState(startupTimeout);
        }
    }

    [Fact] void should_fail_one_partition() => Context.FailedPartitionsBeforeRetry.Count().ShouldEqual(1);
    [Fact] void should_start_replaying_job() => Context.Jobs.First().Type.Value.ShouldContain(nameof(RetryFailedPartition));
    [Fact] void should_recover_failed_partition() => Context.FailedPartitionsAfterRetry.ShouldBeEmpty();
    [Fact] void should_have_the_active_observer_running_state() => Context.ReducerState.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_have_correct_last_handled_event_sequence_number() => Context.ReducerState.LastHandledEventSequenceNumber.Value.ShouldEqual(0ul);
    [Fact] void should_have_correct_next_event_sequence_number() => Context.ReducerState.NextEventSequenceNumber.Value.ShouldEqual(1ul);
}
