// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Storage.Observation;
using Humanizer;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_Reducers.when_handling_event.and_it_fails.and_needs_to_catch_up.context;
using ObserverRunningState = Cratis.Chronicle.Concepts.Observation.ObserverRunningState;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reducers.when_handling_event.and_it_fails;

[Collection(GlobalCollection.Name)]
public class and_needs_to_catch_up(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.a_reducer_observing_an_event_that_can_fail(globalFixture, 3)
    {
        public IEnumerable<FailedPartition> FailedPartitionsBeforeRetry;
        public IEnumerable<FailedPartition> FailedPartitionsAfterRetry;
        public IEnumerable<Job> Jobs;
        public IEnumerable<Job> JobsWithCatchUp;
        public IEnumerable<Job> JobsAfterCompleted;
        public SomeEvent Event;
        public EventSourceId EventSourceId;
        public ObserverState ObserverState;

        void Establish()
        {
            EventSourceId = "some source";
            Event = new SomeEvent(42);
        }

        async Task Because()
        {
            var waitTime = 20.Seconds();
            await ReducerObserver.WaitTillActive(waitTime);
            Observers[0].ShouldFail = true;
            Observers[1].ShouldFail = false;
            Observers[1].HandleTime = 1.Seconds();
            Observers[2].ShouldFail = false;
            Observers[2].HandleTime = 1.Seconds();
            await EventStore.EventLog.Append(EventSourceId, Event);

            // Wait for the first event to have been handled
            await Tcs[0].Task.WaitAsync(waitTime);

            FailedPartitionsBeforeRetry = await EventStore.WaitForThereToBeFailedPartitions(ObserverId);
            Jobs = await EventStore.WaitForThereToBeJobs(waitTime);

            // Wait for the second event to have been handled
            await Tcs[1].Task.WaitAsync(waitTime);

            // Append an event to be caught up
            await EventStore.EventLog.Append(EventSourceId, Event);

            // Wait for the third event to have been handled
            await Tcs[2].Task.WaitAsync(waitTime);
            JobsWithCatchUp = await EventStore.WaitForThereToBeJobs(waitTime);
            JobsAfterCompleted = await EventStore.WaitForThereToBeNoJobs(waitTime);

            FailedPartitionsAfterRetry = await GetFailedPartitions();
            ObserverState = await ReducerObserver.GetState();
            await Observers[2].WaitTillHandledEventReaches(1);
        }
    }

    [Fact] void should_fail_one_partition() => Context.FailedPartitionsBeforeRetry.Count().ShouldEqual(1);
    [Fact] void should_start_replaying_job() => Context.Jobs.First().Type.ShouldContain(nameof(RetryFailedPartition));
    [Fact] void should_recover_failed_partition() => Context.FailedPartitionsAfterRetry.ShouldBeEmpty();
    [Fact] void should_start_catchup_for_partition_job() => Context.JobsWithCatchUp.SingleOrDefault(_ => _.Type == nameof(CatchUpObserverPartition)).ShouldNotBeNull();
    [Fact] void should_have_completed_all_jobs_at_the_end() => Context.JobsAfterCompleted.ShouldBeEmpty();
    [Fact] void should_have_the_active_observer_running_state() => Context.ObserverState.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_have_correct_last_handled_event_sequence_number() => Context.ObserverState.LastHandledEventSequenceNumber.Value.ShouldEqual(1ul);
    [Fact] void should_have_correct_next_event_sequence_number() => Context.ObserverState.NextEventSequenceNumber.Value.ShouldEqual(2ul);
}
