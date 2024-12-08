// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Base;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_handling_event.and_it_fails_first_time_but_not_second_time.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_handling_event;

[Collection(GlobalCollection.Name)]
public class and_it_fails_first_time_but_not_second_time(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.a_reactor_observing_an_event_that_can_fail(globalFixture)
    {
        public IEnumerable<FailedPartition> FailedPartitionsBeforeRetry;
        public IEnumerable<FailedPartition> FailedPartitionsAfterRetry;
        public IEnumerable<Job> Jobs;
        public SomeEvent Event;
        public EventSourceId EventSourceId;

        void Establish()
        {
            EventSourceId = "some source";
            Event = new SomeEvent(42);
        }

        async Task Because()
        {
            await ReactorObserver.WaitTillActive();
            Observer.ShouldFail = true;
            await EventStore.EventLog.Append(EventSourceId, Event);
            await Tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Jobs = await EventStore.WaitForThereToBeJobs();
            FailedPartitionsBeforeRetry = await GetFailedPartitions();
            await EventStore.WaitForThereToBeNoJobs();

            FailedPartitionsAfterRetry = await GetFailedPartitions();
        }
    }

    [Fact] void should_fail_one_partition() => Context.FailedPartitionsBeforeRetry.Count().ShouldEqual(1);
    [Fact] void should_start_replaying_job() => Context.Jobs.First().Type.ShouldContain("RetryFailedPartitionJob");
    [Fact] void should_recover_failed_partition() => Context.FailedPartitionsAfterRetry.ShouldBeEmpty();
}
