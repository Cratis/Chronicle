// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.Specifications.for_Reactors.when_handling_event.and_it_fails.but_not_second_time.context;

namespace Cratis.Chronicle.Integration.Specifications.for_Reactors.when_handling_event.and_it_fails;

[Collection(ChronicleCollection.Name)]
[Trait("Category", "Output")]
public class but_not_second_time(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_reactor_observing_an_event_that_can_fail(chronicleFixture, 2)
    {
        public IEnumerable<FailedPartition> FailedPartitionsBeforeRetry;
        public IEnumerable<FailedPartition> FailedPartitionsAfterRetry;
        public IEnumerable<Job> Jobs;
        public SomeEvent Event;
        public EventSourceId EventSourceId;
        public ReactorState ReactorState;

        void Establish()
        {
            EventSourceId = "some source";
            Event = new SomeEvent(42);
        }

        async Task Because()
        {
            var reactor = EventStore.Reactors.GetHandlerFor<ReactorThatCanFail>();
            await reactor.WaitTillActive();
            Observers[0].ShouldFail = true;
            Observers[1].ShouldFail = false;
            await EventStore.EventLog.Append(EventSourceId, Event);

            // Wait for the first event to have been handled
            await Tcs[0].Task.WaitAsync(TimeSpanFactory.DefaultTimeout());

            FailedPartitionsBeforeRetry = await reactor.WaitForThereToBeFailedPartitions();
            Jobs = await EventStore.Jobs.WaitForThereToBeJobs(TimeSpanFactory.DefaultTimeout());

            // Wait for the second event to have been handled
            await Tcs[1].Task.WaitAsync(TimeSpanFactory.DefaultTimeout());
            await EventStore.Jobs.WaitForThereToBeNoJobs();
            await Observers[1].WaitTillHandledEventReaches(1);

            FailedPartitionsAfterRetry = await GetFailedPartitions();
            ReactorState = await reactor.GetState();
        }
    }

    [Fact] void should_fail_one_partition() => Context.FailedPartitionsBeforeRetry.Count().ShouldEqual(1);
    [Fact] void should_start_replaying_job() => Context.Jobs.First().Type.Value.ShouldContain(nameof(RetryFailedPartition));
    [Fact] void should_recover_failed_partition() => Context.FailedPartitionsAfterRetry.ShouldBeEmpty();
    [Fact] void should_have_the_active_observer_running_state() => Context.ReactorState.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_have_correct_last_handled_event_sequence_number() => Context.ReactorState.LastHandledEventSequenceNumber.Value.ShouldEqual(0ul);
    [Fact] void should_have_correct_next_event_sequence_number() => Context.ReactorState.NextEventSequenceNumber.Value.ShouldEqual(1ul);
}
