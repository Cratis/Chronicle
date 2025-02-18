// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Storage.Observation;
using Humanizer;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_handling_event.and_it_fails.but_not_third_time.context;
using ObserverRunningState = Cratis.Chronicle.Concepts.Observation.ObserverRunningState;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_handling_event.and_it_fails;

[Collection(GlobalCollection.Name)]
[Trait("Category", "Output")]
public class but_not_third_time(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.a_reactor_observing_an_event_that_can_fail(globalFixture, 3)
    {
        public IEnumerable<FailedPartition> FailedPartitionsBeforeRetry;
        public IEnumerable<FailedPartition> FailedPartitionsAfterRetry;
        public IEnumerable<Job> Jobs;
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
            var waitTime = 10.Seconds();

            await ReactorObserver.WaitTillActive();
            Observers[0].ShouldFail = true;
            Observers[1].ShouldFail = true;
            Observers[2].ShouldFail = false;
            await EventStore.EventLog.Append(EventSourceId, Event);

            // Wait for the first event to have been handled
            await Tcs[0].Task.WaitAsync(waitTime);

            FailedPartitionsBeforeRetry = await EventStore.WaitForThereToBeFailedPartitions(ObserverId, waitTime);
            Jobs = await EventStore.WaitForThereToBeJobs(waitTime);

            // Wait for the second event to have been handled
            await Tcs[1].Task.WaitAsync(waitTime);

            FailedPartitionsBeforeRetry = await EventStore.WaitForThereToBeFailedPartitions(ObserverId, TimeSpan.FromSeconds(60));
            Jobs = await EventStore.WaitForThereToBeJobs(waitTime);

            // Wait for the third event to have been handled
            await Tcs[2].Task.WaitAsync(waitTime);
            await EventStore.WaitForThereToBeNoJobs(waitTime);
            await Observers[2].WaitTillHandledEventReaches(1);

            FailedPartitionsAfterRetry = await GetFailedPartitions();
            ObserverState = await ReactorObserver.GetState();
        }
    }

    [Fact] void should_fail_one_partition() => Context.FailedPartitionsBeforeRetry.Count().ShouldEqual(1);
    [Fact] void should_start_replaying_job() => Context.Jobs.First().Type.ShouldContain("RetryFailedPartition");
    [Fact] void should_recover_failed_partition() => Context.FailedPartitionsAfterRetry.ShouldBeEmpty();
    [Fact] void should_have_the_active_observer_running_state() => Context.ObserverState.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_have_correct_last_handled_event_sequence_number() => Context.ObserverState.LastHandledEventSequenceNumber.Value.ShouldEqual(0ul);
    [Fact] void should_have_correct_next_event_sequence_number() => Context.ObserverState.NextEventSequenceNumber.Value.ShouldEqual(1ul);
}
