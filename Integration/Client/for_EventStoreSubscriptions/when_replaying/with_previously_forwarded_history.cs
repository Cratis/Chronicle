// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;
using context = Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_replaying.with_previously_forwarded_history.context;

namespace Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_replaying;

[Collection(ChronicleCollection.Name)]
public class with_previously_forwarded_history(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_subscription_with_retained_outbox_history(fixture)
    {
        public bool ReplayCompleted;

        async Task Because()
        {
            await SubscribeAndDeliverHistory();
            var job = await Forwarder.Replay();
            job.Value.ShouldNotEqual(Guid.Empty);
            await WaitForDeliveries(2);
            var completed = await Source.Jobs.WaitTillJobCompletesOrIsDeleted(new JobId(job.Value), Timeout);
            completed?.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
            using var cancellation = new CancellationTokenSource(Timeout);
            while ((await Forwarder.GetState()).IsReplaying)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50), cancellation.Token);
            }
            ReplayCompleted = true;
            await ReadInbox();
        }
    }

    [Fact] void should_finish_the_actual_replay_job() => Context.ReplayCompleted.ShouldBeTrue();
    [Fact] void should_append_four_inbox_rows_not_two() => Context.StoredEvents.Count.ShouldEqual(4);
    [Fact] void should_append_the_first_value_again() => Context.StoredValues().Count(_ => _ == "first").ShouldEqual(2);
    [Fact] void should_append_the_second_value_again() => Context.StoredValues().Count(_ => _ == "second").ShouldEqual(2);
    [Fact] void should_give_the_replayed_rows_new_inbox_sequence_numbers() => Context.StoredEvents.Select(_ => _.Context.SequenceNumber.Value).ShouldContainOnly([0UL, 1UL, 2UL, 3UL]);
}
