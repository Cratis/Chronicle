// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using context = Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_resubscribing.with_retained_subscription_progress.context;

namespace Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_resubscribing;

[Collection(ChronicleCollection.Name)]
public class with_retained_subscription_progress(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_subscription_with_retained_outbox_history(fixture)
    {
        public bool WasUnsubscribed;
        public bool IsResubscribed;
        public ulong RetainedCheckpoint;
        public ulong FinalCheckpoint;

        async Task Because()
        {
            await SubscribeAndDeliverHistory();
            RetainedCheckpoint = (await Forwarder.GetState()).LastHandledEventSequenceNumber.Value;
            await Forwarder.Unsubscribe();
            WasUnsubscribed = !await Forwarder.IsSubscribed();
            var next = await Source.GetEventSequence(EventSequenceId.Outbox).Append("third-source", new given.SubscriptionHistoryPublished("third"));
            next.IsSuccess.ShouldBeTrue();
            await Subscribe();
            await WaitForDeliveries(1);
            (await next.WaitForCompletion(Timeout)).IsSuccess.ShouldBeTrue();
            await WaitForForwarderCheckpoint(next.SequenceNumber.Value);
            IsResubscribed = await Forwarder.IsSubscribed();
            FinalCheckpoint = (await Forwarder.GetState()).LastHandledEventSequenceNumber.Value;
            await ReadInbox();
        }
    }

    [Fact] void should_really_disconnect_the_forwarder() => Context.WasUnsubscribed.ShouldBeTrue();
    [Fact] void should_reconnect_the_same_forwarder() => Context.IsResubscribed.ShouldBeTrue();
    [Fact] void should_retain_the_historical_checkpoint() => Context.RetainedCheckpoint.ShouldEqual(1UL);
    [Fact] void should_advance_only_to_the_new_event() => Context.FinalCheckpoint.ShouldEqual(2UL);
    [Fact] void should_not_append_the_old_history_again() => Context.StoredEvents.Count.ShouldEqual(3);
    [Fact] void should_forward_each_value_exactly_once() => Context.StoredValues().ShouldContainOnly(["first", "second", "third"]);
}
